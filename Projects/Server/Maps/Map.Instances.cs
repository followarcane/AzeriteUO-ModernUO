using System;

namespace Server;

public sealed partial class Map
{
    public int InstanceId { get; private init; }
    public Map BaseFacet => _baseFacet ?? this;
    private Map _baseFacet;

    // Save files store a byte-sized map index. Once used, a world's six slots must never be reassigned.
    public static void RegisterWorldInstance(int instanceId, int firstMapIndex)
    {
        const int facetCount = 6;
        if (instanceId <= 0 || Felucca?.GetInstance(instanceId) != null)
        {
            throw new ArgumentException("World instance IDs must be positive and unique.", nameof(instanceId));
        }

        if (firstMapIndex is < 32 or > 249 || firstMapIndex <= 127 && firstMapIndex + facetCount > 127)
        {
            throw new ArgumentOutOfRangeException(nameof(firstMapIndex));
        }

        for (var i = 0; i < facetCount; i++)
        {
            if (Maps[i] == null || Maps[firstMapIndex + i] != null)
            {
                throw new InvalidOperationException($"Missing base facet or occupied map slot for facet {i}.");
            }
        }

        for (var i = 0; i < facetCount; i++)
        {
            var facet = Maps[i];
            var index = firstMapIndex + i;
            var map = new Map(facet.MapID, index, facet.m_FileIndex, facet.Width, facet.Height, facet.Season,
                $"{facet.Name}-{instanceId}", facet.Rules) { InstanceId = instanceId, _baseFacet = facet };
            Maps[index] = map;
            AllMaps.Add(map);
        }
    }

    public Map GetInstance(int id)
    {
        if (id == 0)
        {
            return BaseFacet;
        }

        foreach (var map in AllMaps)
        {
            if (map.InstanceId == id && map.BaseFacet == BaseFacet)
            {
                return map;
            }
        }

        return null;
    }
}
