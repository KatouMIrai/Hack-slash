using System.Collections.Generic;
using UnityEngine;

namespace WeaponMazeAlchemy.Prototype
{
    public static class FloorTemplateDatabase
    {
        private static readonly List<FloorTemplate> defaultTemplates = new List<FloorTemplate>
        {
            new FloorTemplate(
                "Small Crossroad",
                "##########",
                "#P.......#",
                "#..##....#",
                "#.....S..#",
                "#....##..#",
                "#......E.#",
                "##########"),
            new FloorTemplate(
                "Broken Hall",
                "############",
                "#P.....#...#",
                "#......#...#",
                "#..##......#",
                "#......##S.#",
                "#...E....E.#",
                "############"),
            new FloorTemplate(
                "Bent Passage",
                "############",
                "#P.........#",
                "#.####.....#",
                "#....#.....#",
                "#....#..ES.#",
                "#.........E#",
                "############")
        };

        public static IReadOnlyList<FloorTemplate> DefaultTemplates => defaultTemplates;

        public static FloorTemplate PickRandomDefault()
        {
            return defaultTemplates[Random.Range(0, defaultTemplates.Count)];
        }
    }
}
