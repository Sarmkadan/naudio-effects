// Copyright (c) 2023-present NAudioEffects Contributors. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;

namespace NAudioEffects
{
    /// <summary>
    /// Provides validation helpers for <see cref="SilenceDetector"/> instances.
    /// </summary>
    public static class SilenceDetectorValidation
    {
        /// <summary>
        /// Validates a <see cref="SilenceDetector"/> instance and returns a list of validation problems.
        /// </summary>
        /// <param name="value">The silence detector to validate.</param>
        /// <returns>A read-only list of validation problems; empty if the instance is valid.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
        public static IReadOnlyList<string> Validate(this SilenceDetector? value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var problems = new List<string>();

            // Validate Regions collection
            if (value.Regions == null)
            {
                problems.Add("Regions collection cannot be null.");
            }
            else
            {
                // Validate each SilenceRegion
                for (int i = 0; i < value.Regions.Count; i++)
                {
                    var region = value.Regions[i];
                    if (region == null)
                    {
                        problems.Add($"Regions[{i}] cannot be null.");
                        continue;
                    }

                    // Validate SilenceRegion.Start
                    if (region.Start < TimeSpan.Zero)
                    {
                        problems.Add($"Regions[{i}].Start cannot be negative. Actual: {region.Start}.");
                    }

                    // Validate SilenceRegion.End
                    if (region.End < TimeSpan.Zero)
                    {
                        problems.Add($"Regions[{i}].End cannot be negative. Actual: {region.End}.");
                    }

                    // Validate SilenceRegion.Start <= SilenceRegion.End
                    if (region.Start > region.End)
                    {
                        problems.Add($"Regions[{i}].Start ({region.Start}) cannot be greater than End ({region.End}).");
                    }

                    // Validate SilenceRegion.Duration is reasonable
                    var duration = region.Duration;
                    if (duration < TimeSpan.Zero)
                    {
                        problems.Add($"Regions[{i}].Duration cannot be negative. Actual: {duration}.");
                    }
                }
            }

            // Validate TotalDuration
            if (value.TotalDuration < TimeSpan.Zero)
            {
                problems.Add($"TotalDuration cannot be negative. Actual: {value.TotalDuration}.");
            }

            return problems.AsReadOnly();
        }

        /// <summary>
        /// Determines whether a <see cref="SilenceDetector"/> instance is valid.
        /// </summary>
        /// <param name="value">The silence detector to check.</param>
        /// <returns><see langword="true"/> if the instance is valid; otherwise, <see langword="false"/>.</returns>
        public static bool IsValid(this SilenceDetector? value)
        {
            return value is { } detector && Validate(detector).Count == 0;
        }

        /// <summary>
        /// Ensures that a <see cref="SilenceDetector"/> instance is valid.
        /// </summary>
        /// <param name="value">The silence detector to validate.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the instance is invalid, containing a list of validation problems.</exception>
        public static void EnsureValid(this SilenceDetector? value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var problems = Validate(value);
            if (problems.Count > 0)
            {
                throw new ArgumentException(
                    $"SilenceDetector is invalid.{Environment.NewLine}- {
                    string.Join($"{Environment.NewLine}- ", problems)
                    }");
            }
        }
    /// <summary>
/// Validates a <see cref="SilenceDetector.SilenceRegion"/> instance and returns a list of validation problems.
/// </summary>
/// <param name="value">The silence region to validate.</param>
/// <returns>A read-only list of validation problems; empty if the region is valid.</returns>
/// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
public static IReadOnlyList<string> Validate(this SilenceDetector.SilenceRegion? value)
{
	ArgumentNullException.ThrowIfNull(value);

	var problems = new List<string>();

	// Validate Start
	if (value.Start < TimeSpan.Zero)
	{
		problems.Add($"Start cannot be negative. Actual: {value.Start}.");
	}

	// Validate End
	if (value.End < TimeSpan.Zero)
	{
		problems.Add($"End cannot be negative. Actual: {value.End}.");
	}

	// Validate Start <= End
	if (value.Start > value.End)
	{
		problems.Add($"Start ({value.Start}) cannot be greater than End ({value.End}).");
	}

	// Validate Duration is reasonable (should never be negative based on above checks)
	var duration = value.Duration;
	if (duration < TimeSpan.Zero)
	{
		problems.Add($"Duration cannot be negative. Actual: {duration}.");
	}

	return problems.AsReadOnly();
}

/// <summary>
/// Determines whether a <see cref="SilenceDetector.SilenceRegion"/> instance is valid.
/// </summary>
/// <param name="value">The silence region to check.</param>
/// <returns><see langword="true"/> if the region is valid; otherwise, <see langword="false"/>.</returns>
public static bool IsValid(this SilenceDetector.SilenceRegion? value)
{
	return value is { } region && Validate(region).Count == 0;
}

/// <summary>
/// Ensures that a <see cref="SilenceDetector.SilenceRegion"/> instance is valid.
/// </summary>
/// <param name="value">The silence region to validate.</param>
/// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
/// <exception cref="ArgumentException">Thrown when the region is invalid, containing a list of validation problems.</exception>
public static void EnsureValid(this SilenceDetector.SilenceRegion? value)
{
	ArgumentNullException.ThrowIfNull(value);

	var problems = Validate(value);
	if (problems.Count > 0)
	{
		throw new ArgumentException(
			$"SilenceRegion is invalid.{Environment.NewLine}- {
			string.Join($"{Environment.NewLine}- ", problems)
		}");
	}
}

}
}