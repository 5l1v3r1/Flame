﻿using System;
using System.Collections.Generic;
using System.Linq;
using Loyc.MiniTest;
using Flame;
using Flame.Collections;

namespace UnitTests
{
    [TestFixture]
    public class CacheTests
    {
        public CacheTests(Random rng)
        {
            this.rng = rng;
        }

        private Random rng;

        private void TestCache<TKey, TValue>(
            Cache<TKey, TValue> cache,
            Func<Random, TKey> generateKey,
            Func<Random, TValue> generateValue,
            bool relaxHasKey)
        {
            var dict = new Dictionary<TKey, TValue>();
            const int opCount = 200000;
            for (int i = 0; i < opCount; i++)
            {
                switch (rng.Next(3))
                {
                    case 0:
                    {
                        // Perform an insertion.
                        var key = generateKey(rng);
                        var value = generateValue(rng);
                        dict[key] = value;
                        cache.Insert(key, value);
                        break;
                    }
                    case 1:
                    {
                        // Perform a try-get operation.
                        var key = generateKey(rng);
                        TValue value, cacheValue;
                        var hasKey = dict.TryGetValue(key, out value);
                        var cacheHasKey = cache.TryGet(key, out cacheValue);
                        if (!relaxHasKey)
                        {
                            Assert.AreEqual(
                                hasKey,
                                cacheHasKey,
                                "Try-get operation error: cache says " + 
                                "it does not contain key '" + key +
                                "', but it should.");
                        }
                        if (cacheHasKey)
                        {
                            Assert.AreEqual(
                                value,
                                cacheValue,
                                "Try-get operation error: cached value '" + cacheValue +
                                "' does not match actual value '" + value +
                                "' (key: '" + key + "').");
                        }
                        break;
                    }
                    case 2:
                    {
                        // Perform a get operation.
                        var key = generateKey(rng);
                        TValue value;
                        if (!dict.TryGetValue(key, out value))
                        {
                            value = generateValue(rng);
                            dict[key] = value;
                        }
                        var cacheValue = cache.Get(key, new ConstantFunction<TKey, TValue>(value).Apply);
                        Assert.AreEqual(
                            value,
                            cacheValue,
                            "Get operation error: cached value '" + cacheValue +
                            "' does not match actual value '" + value +
                            "' (key: '" + key + "').");
                        break;
                    }
                }
            }
        }

        [Test]
        public void LruCache()
        {
            TestCache<int, int>(
                new LruCache<int, int>(128),
                GenerateInt32,
                GenerateInt32,
                true);
        }

        private int GenerateInt32(Random rng)
        {
            return rng.Next(short.MinValue, short.MaxValue);
        }
    }

    internal class ConstantFunction<T1, T2>
    {
        public ConstantFunction(T2 result)
        {
            this.Result = result;
        }

        public T2 Result { get; private set; }

        public T2 Apply(T1 arg)
        {
            return Result;
        }
    }
}
