using System;
using System.Threading;
using BepInEx;
using JetBrains.Annotations;
using UnityEngine;

namespace GeBoCommon.Utilities
{
    public class HitMissCounter
    {
        private readonly string _name;
        private long _cacheHits;
        private float _cacheHitTime;
        private long _cacheMisses;
        private float _cacheMissTime;

        public HitMissCounter(string name)
        {
            _name = name;
            Reset();
        }

        [PublicAPI]
        public void Reset()
        {
            _cacheHits = _cacheMisses = 0;
            _cacheHitTime = _cacheMissTime = 0f;
        }

        private static void IncrementCount(ref long count)
        {
            Interlocked.Increment(ref count);
        }

        private static void IncrementElapsedTime(ref float cacheTime, float elapsed)
        {
            var nextValue = cacheTime;
            while (true)
            {
                var currentValue = nextValue;
                var newTime = nextValue + elapsed;
                nextValue = Interlocked.CompareExchange(ref cacheTime, newTime, currentValue);
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (nextValue == newTime) return;
            }
        }

        public void RecordHit()
        {
            Action Worker()
            {
                IncrementCount(ref _cacheHits);
                return null;
            }

            ThreadingHelper.Instance.StartAsyncInvoke(Worker);
        }


        public void RecordHit(float startTime)
        {
            var elapsed = Time.realtimeSinceStartup - startTime;

            Action Worker()
            {
                IncrementCount(ref _cacheHits);
                IncrementElapsedTime(ref _cacheHitTime, elapsed);
                return null;
            }

            ThreadingHelper.Instance.StartAsyncInvoke(Worker);
        }

        public void RecordMiss()
        {
            Action Worker()
            {
                IncrementCount(ref _cacheMisses);
                return null;
            }

            ThreadingHelper.Instance.StartAsyncInvoke(Worker);
        }


        public void RecordMiss(float startTime)
        {
            var elapsed = Time.realtimeSinceStartup - startTime;

            Action Worker()
            {
                IncrementCount(ref _cacheMisses);
                IncrementElapsedTime(ref _cacheMissTime, elapsed);
                return null;
            }

            ThreadingHelper.Instance.StartAsyncInvoke(Worker);
        }


        public void RecordHitTime(float startTime)
        {
            var elapsed = Time.realtimeSinceStartup - startTime;

            Action Worker()
            {
                IncrementElapsedTime(ref _cacheHitTime, elapsed);
                return null;
            }

            ThreadingHelper.Instance.StartAsyncInvoke(Worker);
        }

        public void RecordMissTime(float startTime)
        {
            var elapsed = Time.realtimeSinceStartup - startTime;

            Action Worker()
            {
                IncrementElapsedTime(ref _cacheMissTime, elapsed);
                return null;
            }

            ThreadingHelper.Instance.StartAsyncInvoke(Worker);
        }

        public string GetCounts(string prefix, int count = -1)
        {
            var msgBuilder = StringBuilderPool.Get();
            var hits = Interlocked.Read(ref _cacheHits);
            var hitTime = _cacheHitTime;
            var misses = Interlocked.Read(ref _cacheMisses);
            var missTime = _cacheMissTime;

            try
            {
                msgBuilder
                    .Append(nameof(HitMissCounter)).Append('.').Append(nameof(GetCounts)).Append(": ")
                    .Append('"').Append(_name).Append('"').Append(',')
                    .Append('"').Append(prefix).Append('"').Append(',')
                    .Append(hits).Append(',')
                    .AppendFormat("{0:0.00000}", CacheTimeInMicroseconds(hitTime, hits)).Append(',')
                    .Append(misses).Append(',')
                    .AppendFormat("{0:0.00000}", CacheTimeInMicroseconds(missTime, misses)).Append(',')
                    .Append(hits + misses).Append(',')
                    .AppendFormat("{0:0.00000}",
                        CacheTimeInMicroseconds(hitTime + missTime, hits + misses)).Append(',');
                if (count >= 0) msgBuilder.Append(count);
                return msgBuilder.ToString();
            }
            finally
            {
                StringBuilderPool.Release(msgBuilder);
            }
        }

        private static float CacheTimeInMicroseconds(float totalElapsed, long count)
        {
            return count == 0 ? 0f : totalElapsed * 1_000_000f / count;
        }
    }
}
