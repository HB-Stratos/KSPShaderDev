#define UINT_MAX 4294967295u

#if defined( PARTICLE_MIDINCLUDE )
#undef PARTICLE_MIDINCLUDE




RWStructuredBuffer<int> _AvailableIndices;

ConsumeStructuredBuffer<int> _AliveIndices;
AppendStructuredBuffer<int> _NextAliveIndices;



RWStructuredBuffer<Particle> _ParticleData;
#ifdef PARTICLE_USE_PARENTING
RWStructuredBuffer<Particle> _NextParticleData;
#endif
uniform uint _ParticleDataSize;
uniform uint _ParticleDataStride;

RWStructuredBuffer<int> _UpdateIndArgs;

struct CustomData{
    uint _ParticleDataSize;
    uint _ParticleDataStride;
    uint _CurrentParticleCount;
    uint _AvailableIndex;
};

RWStructuredBuffer<CustomData> _CustomData < int requestedSize = 4 ; >;
/// [0] -> _ParticleDataSize
/// [1] -> _ParticleDataStride
/// [2] -> _CurrentParticleCount


uint GetRandomSeed(){
    return _ParticleData.IncrementCounter();
}

bool getParent(Particle child, out Particle parent){
}


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
    if (newParticleIndex == UINT_MAX) return;
    
    //new particle index can also be used for parenting, would require multiple emits in the same call though
    p.index = newParticleIndex;
    #ifdef PARTICLE_USE_PARENTING
    _NextParticleData[newParticleIndex] = p;
    #else
    _ParticleData[newParticleIndex] = p;
    #endif
}


#elif defined ( PARTICLE_POSTINCLUDE )
#undef PARTICLE_POSTINCLUDE

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
    if (id.x >= _CustomData[0]._CurrentParticleCount) return;

    uint processedParticleIndex = _AliveIndices.Consume();
    Particle p = _ParticleData[processedParticleIndex];
    if (Update(p)) _NextAliveIndices.Append(processedParticleIndex);
    else {
        uint deadParticleWriteIndex = _AvailableIndices.IncrementCounter();
        _AvailableIndices[deadParticleWriteIndex] = processedParticleIndex;
        return;
    }

    #ifdef PARTICLE_USE_PARENTING
    _NextParticleData[processedParticleIndex] = p;
    #else
    _ParticleData[processedParticleIndex] = p;
    #endif
}

[numthreads(1, 1, 1)]
void _EarlyUpdate()
{
    _CustomData[0]._CurrentParticleCount = _UpdateIndArgs[0]; 
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
        _CustomData[0]._ParticleDataSize = numStructs;
        _CustomData[0]._ParticleDataStride = stride;
    }
    
    _AvailableIndices[id.x] = id.x; //TODO WARNING out of bounds write, probably fine only in dx11

}


#endif