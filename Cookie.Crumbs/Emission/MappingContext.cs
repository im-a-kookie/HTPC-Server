using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Cookie.Emission.ParameterMapping;

namespace Cookie.Emission
{
    /// <summary>
    /// Encapsulation context for mapping entry->target parameter signatures
    /// </summary>
    public class MappingContext
    {
        /// <summary>
        /// The entry parameters provided to the mapping algorithm
        /// </summary>
        public Type[] EntryParameters { get; set; }

        /// <summary>
        /// The target parameters expected to be mapped from the entry
        /// </summary>
        public Type[] TargetParameters { get; set; }

        /// <summary>
        /// Dictionary mapping <see cref="TargetParameters"/> index to <see cref="EntryParameters"/> index (or -1 for null/default).
        /// </summary>
        public Dictionary<int, int> Mappings { get; set; }

        /// <summary>
        /// A flag array indicating the consumption state of entry parameters
        /// </summary>
        public bool[] SolvedEntries { get; set; }

        /// <summary>
        /// A flag array indicating the consumption state of target parameters
        /// </summary>
        public bool[] SolvedTargets { get; set; }

        /// <summary>
        /// Whether reversible assignability is permitted from Target->Entry.
        /// 
        /// <para>This allows the target to be more specific about typing than the entry delegate.</para>
        /// </summary>
        public bool ReversibleAssignability { get; set; } = true;


        /// <summary>
        /// Initializes a new instance of the <see cref="MappingContext"/> class.
        /// </summary>
        /// <param name="entryParameters">The parameters for the entry call.</param>
        /// <param name="targetParameters">The parameters for the target call.</param>
        public MappingContext(Type[] entryParameters, Type[] targetParameters)
        {
            EmissionErrors.NullMethodParameter.AssertNotNull(entryParameters, "No Entry signature");
            EmissionErrors.NullMethodParameter.AssertNotNull(targetParameters, "No Target signature");

            if (entryParameters.Contains(null))
                throw EmissionErrors.NullMethodParameter.Get("Entry contains null parameter)");

            if (targetParameters.Contains(null))
                throw EmissionErrors.NullMethodParameter.Get("Target contains null parameter");

            EntryParameters = entryParameters;
            TargetParameters = targetParameters;
            Mappings = new Dictionary<int, int>(); // Initialize the dictionary to hold mappings
            SolvedEntries = new bool[entryParameters.Length]; // Initialize input flags array
            SolvedTargets = new bool[targetParameters.Length]; // Initialize output flags array
        }

        /// <summary>
        /// Maps the output index to the input index and consumes both parameters
        /// </summary>
        /// <param name="target"></param>
        /// <param name="entry"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void Map(int target, int entry)
        {
            // Validate the indices
            // Remmeber that -1 is permitted to indicate nullification
            if (entry >= EntryParameters.Length || (entry < 0 && entry != -1))
                throw EmissionErrors.MappingInvalidIndex.Get($"(entry: {entry})");

            if (target < 0 || target >= TargetParameters.Length)
                throw EmissionErrors.MappingInvalidIndex.Get($"(target: {target})");

            if (SolvedTargets[target])
                throw EmissionErrors.TargetAlreadyMapped.Get($"(target: {target})");

            // Now we can fill it
            Mappings.TryAdd(target, entry);
            if (entry >= 0) SolvedEntries[entry] = true; //already checked i>len
            SolvedTargets[target] = true;
        }

        /// <summary>
        /// Computes this context into a list of mappings, ordered by output parameter index.
        /// </summary>
        /// <returns>A sorted result of mapping structs</returns>
        public List<Mapping> ComputeSortedMapping(bool nullifyEmptyOutputs = true)
        {
            for (int o = 0; o < TargetParameters.Length; ++o)
            {
                if (!SolvedTargets[o])
                {
                    if (nullifyEmptyOutputs)
                    {
                        ParameterMapping.FillUnmappedOutputs(this);
                        break;
                    }
                    else
                    {
                        throw EmissionErrors.UnmappedTargets.Get($"index = {o}");
                    }
                }
            }

            // Now just map and done
            return Mappings
            .OrderBy(x => x.Key) // Sort by Value in ascending order
            .Select(x => new Mapping(x.Value, x.Key))
            .ToList();
        }

    }

}
