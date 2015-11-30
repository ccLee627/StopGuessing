﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace StopGuessing.DataStructures
{
    public interface IFrequencies
    {
        Proportion[] Proportions { get; }

        Task RecordObservationAsync(
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default(CancellationToken));
    }

    public interface IFrequenciesProvider<in TKey>
    {
        Task<IFrequencies> GetFrequenciesAsync(
            TKey key,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default(CancellationToken));
    }

    public class MultiperiodFrequencyTracker<TKey> : IFrequenciesProvider<TKey>
    {
        ///// <summary>
        ///// We track a sedquence of unsalted failed passwords so that we can determine their pouplarity
        ///// within different historical frequencies.  We need this sequence because to know how often
        ///// a password occurred among the past n failed passwords, we need to add a count each time we
        ///// see it and remove the count when n new failed passwords have been recorded. 
        ///// </summary>
        protected List<FrequencyTracker<TKey>> PasswordFrequencyEstimatesForDifferentPeriods;

        //public uint[] LengthOfPopularityMeasurementPeriods;

        public MultiperiodFrequencyTracker(int numberOfPopularityMeasurementPeriods,
            uint lengthOfShortestPopularityMeasurementPeriod,
            uint factorOfGrowthBetweenPopularityMeasurementPeriods
            )
        {
            //LengthOfPopularityMeasurementPeriods = new uint[numberOfPopularityMeasurementPeriods];
            PasswordFrequencyEstimatesForDifferentPeriods =
                new List<FrequencyTracker<TKey>>(numberOfPopularityMeasurementPeriods);
            uint currentPeriodLength = lengthOfShortestPopularityMeasurementPeriod;
            for (int period = 0; period < numberOfPopularityMeasurementPeriods; period++)
            {
                //LengthOfPopularityMeasurementPeriods[period] = currentPeriodLength;
                PasswordFrequencyEstimatesForDifferentPeriods.Add(
                    new FrequencyTracker<TKey>((int) currentPeriodLength));
                currentPeriodLength *= factorOfGrowthBetweenPopularityMeasurementPeriods;
            }
            // Reverese the frequency trackers so that the one that tracks the most items is first on the list.
            PasswordFrequencyEstimatesForDifferentPeriods.Reverse();

        }

        public Proportion[] Get(TKey key)
        {
            return PasswordFrequencyEstimatesForDifferentPeriods.Select(
                (ft) => ft.Get(key)).ToArray();
        }

        public void RecordObservation(TKey key)
        {
            foreach (FrequencyTracker<TKey> ft in PasswordFrequencyEstimatesForDifferentPeriods)
                ft.Observe(key);
        }

        public async Task<IFrequencies> GetFrequenciesAsync(TKey key,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return await Task.Run(() => new FrequencyTrackerFrequencies(this, key, Get(key)), cancellationToken);
        }
    


    public class FrequencyTrackerFrequencies : IFrequencies
        {
            protected MultiperiodFrequencyTracker<TKey> Tracker;
            protected TKey Key;

            public Proportion[] Proportions { get; protected set; }

            public async Task RecordObservationAsync(
                TimeSpan? timeout = null,
                CancellationToken cancellationToken = default(CancellationToken))
            {
                await Task.Run(() => Tracker.RecordObservation(Key), cancellationToken);
            }

            public FrequencyTrackerFrequencies(MultiperiodFrequencyTracker<TKey> tracker, TKey key, Proportion[] proportions)
            {
                Tracker = tracker;
                Key = key;
                Proportions = proportions;
            }

        }

    }
}