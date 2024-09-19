#define UINT_MAX 4294967295

#if defined( PARTICLE_MIDINCLUDE )
#undef PARTICLE_MIDINCLUDE




RWStructuredBuffer<int> _AvailableIndices;

ConsumeStructuredBuffer<int> _AliveIndices;
AppendStructuredBuffer<int> _NextAliveIndices;



RWStructuredBuffer<Particle> _ParticleData;
uniform uint _ParticleDataSize;
uniform uint _ParticleDataStride;

RWStructuredBuffer<int> _UpdateIndArgs;

RWStructuredBuffer<uint> _CustomConstants < int requestedSize = 3 ; >;
/// [0] -> _ParticleDataSize
/// [1] -> _ParticleDataStride
/// [2] -> _CurrentParticleCount


uint GetRandomSeed(){
    return _ParticleData.IncrementCounter();
}

#elif defined ( PARTICLE_POSTINCLUDE )
#undef PARTICLE_POSTINCLUDE

// int IncrementDataCounter()
// {
//     _ParticleDataInUseCount++;
//     return _ParticleData.IncrementCounter();
// }
// int DecrementDataCounter()
// {
//     _ParticleDataInUseCount--;
//     return _ParticleData.DecrementCounter();
// }



uint GetNewParticleIndex()
{
    uint availableIndicesIndex = _AvailableIndices.DecrementCounter();
    // if (availableIndicesIndex > _ParticleDataSize) return UINT_MAX;
    
    uint newParticleIndex = _AvailableIndices[availableIndicesIndex];
    _NextAliveIndices.Append(newParticleIndex);
    return newParticleIndex;
}

void _Emit(Particle p)
{
    uint newParticleIndex = GetNewParticleIndex();
    // if (newParticleIndex == UINT_MAX) return;
    
    //new particle index can also be used for parenting, would require multiple emits in the same call though
    p.index = newParticleIndex;
    _ParticleData[newParticleIndex] = p;
}

//TODO kinda ugly to have only one thread in group, fix maybe?
//TODO make particle parenting possible
[numthreads(1, 1, 1)]
void _CpuEmit()
{
    
    _Emit(Emit());
}

//TODO this would ideally be (64, 1, 1), but this is not possible with the current copy count.
//TODO WARNING if this was 64 threads I might run too many threads and consume more data than present in the buffer
//TODO ffs this implementation of reading and writing may cause a race condition when accessing parents
[numthreads(64, 1, 1)]
void _Update(uint3 id : SV_DispatchThreadID)
{
    //TODO 64 threads currently breaks because the check needs to go for current particle count
    if (id.x >= _CustomConstants[2]) return;

    uint processedParticleIndex = _AliveIndices.Consume();
    Particle p = _ParticleData[processedParticleIndex];
    if (Update(p)) _NextAliveIndices.Append(processedParticleIndex);
    else {
        uint deadParticleWriteIndex = _AvailableIndices.IncrementCounter();
        _AvailableIndices[deadParticleWriteIndex] = processedParticleIndex;
        return;
    }
    _ParticleData[processedParticleIndex] = p;
}

[numthreads(1, 1, 1)]
void _EarlyUpdate()
{
    _CustomConstants[2] = _UpdateIndArgs[0]; 
    uint threadGroupSize = 64;
    _UpdateIndArgs[0] = (_UpdateIndArgs[0] + (threadGroupSize - 1)) / threadGroupSize;
}

[numthreads(64, 1, 1)] //Initialize the available indices buffer
void _Init(uint3 id : SV_DispatchThreadID)
{
    
    if (id.x == 0)
    {
        uint numStructs;
        uint stride;
        _ParticleData.GetDimensions(numStructs, stride);
        _CustomConstants[0] = numStructs;
        _CustomConstants[1] = stride;
    }
    
    _AvailableIndices[id.x] = id.x; //TODO WARNING out of bounds write, probably fine only in dx11

}


#endif