#define UINT_MAX 4294967295u

#if defined(PARTICLE_MIDINCLUDE)
    #undef PARTICLE_MIDINCLUDE




    RWStructuredBuffer<int> _AvailableIndices;

    ConsumeStructuredBuffer<int> _AliveIndices;
    AppendStructuredBuffer<int> _NextAliveIndices;



    RWStructuredBuffer<Particle> _ParticleData;
    #ifdef PARTICLE_USE_PARENTING
        RWStructuredBuffer<Particle> _NextParticleData;
    #endif

    RWStructuredBuffer<int> _UpdateIndArgs;

    struct CustomData
    {
        uint _ParticleDataSize;
        uint _ParticleDataStride;
        uint _CurrentParticleCount;
        uint _BufferOverrunAmount;
    };

    RWStructuredBuffer<CustomData> _CustomData < int requestedSize = 4; > ;


    uint GetRandomSeed()
    {
        return _ParticleData.IncrementCounter();
    }

    bool GetParent(Particle child, out Particle parent)
    {
        if (child.parent == -1) return false;
        parent = _ParticleData[child.parent];
        return true;
    }


    uint GetNewParticleIndex()
    {
        if (_CustomData[0]._BufferOverrunAmount > 0)
        {
            return UINT_MAX;
        }
        else
        {
            uint availableIndicesIndex = _AvailableIndices.DecrementCounter();
            
            if (availableIndicesIndex > _CustomData[0]._ParticleDataSize)
            {
                InterlockedAdd(_CustomData[0]._BufferOverrunAmount, 1);
                return UINT_MAX;
            }
            else
            {
                uint newParticleIndex = _AvailableIndices[availableIndicesIndex];
                _NextAliveIndices.Append(newParticleIndex);
                return newParticleIndex;
            }
        }
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

    //TODO this discontinuity is dangerous, might lead to allocated but not used indices
    void _Emit(Particle p, uint index)
    {
        p.index = index;
        #ifdef PARTICLE_USE_PARENTING
            _NextParticleData[index] = p;
        #else
            _ParticleData[index] = p;
        #endif
    }


#elif defined(PARTICLE_POSTINCLUDE)
    #undef PARTICLE_POSTINCLUDE

    //TODO kinda ugly to have only one thread in group, fix maybe?
    //TODO make particle parenting possible
    [numthreads(1, 1, 1)]
    void _CpuEmit()
    {
        
        uint newParticleIndex = GetNewParticleIndex();
        if (newParticleIndex == UINT_MAX) return;
        Particle p;
        p.index = newParticleIndex;
        DoEmit(p);
        _Emit(p, newParticleIndex);
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
        if (DoUpdate(p)) _NextAliveIndices.Append(processedParticleIndex);
        else
        {
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

        if (_CustomData[0]._BufferOverrunAmount > 0)
        {
            uint counterValue = _AvailableIndices.IncrementCounter();
            InterlockedAdd(_CustomData[0]._BufferOverrunAmount, -1);
            // if (counterValue <= _CustomData[0]._ParticleDataSize)
            // {
            //     _CustomData[0]._BufferOverrunAmount = 0;
            // }

        }
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
            _CustomData[0]._BufferOverrunAmount = 0;
        }
        
        _AvailableIndices[id.x] = id.x; //TODO WARNING out of bounds write, probably fine only in dx11

    }


#endif