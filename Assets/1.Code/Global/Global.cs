using UnityEngine;

namespace COLUMBARIUM.Global
{
    public enum Chapter
    {
        STARTSCREEN, //0
        PROLOG, //1
        NICHTS, //2
        GARTEN, //3
        GARTEN_INVERSE, //4
        GARTEN_INV_QUESTION, //5
        TAUBENSCHLAG, //6
        TAUBENSCHLAG_QUESTION, //7
        PIGEON, //8
        PIGEON_QUESTION, //9
        TRICKSTER, //10
        EMBRYO, //11
        FAREWELL, //12
        EPILOG, //13
        CREDITS //14
    }




    public static class GlobalProgress
    {
        public static string[] chapterNames =
        {
            "STARTSCREEN",
            "PART 0: PROLOGUE",
            "PART I: NICHTS",
            "PART II: PARADISE GARDEN",
            "PART II.II: PARADISE GARDEN INVERSE",
            "PART II.II.q: INVERSE QUESTION",
            "PART III: DER TAUBENSCHLAG",
            "PART III.p: TAUBENSCHLAG QUESTION",
            "PART IV: BECOMING PIGEON",
            "PART IV.q: PIGEON  QUESTION",
            "PART V: BE A TRICKSTER, BUILD A WORLD",
            "PART VI: EMBRYO",
            "PART VII: FAREWELL",
            "PART VIII: EPILOGUE",
            "PART IX: CREDITS"

        };

        public static bool english = false;

        //This holds the amount of story points for each chapter
        private static int[] storypointCounter = { 0, 0, 0, 5, 3, 0, 7, 0, 6, 0, 5, 0, 0, 0, 0 };

        public static void OverrideStorypointCounter(Chapter chapter, int value)
        {
            switch (chapter)
            {
                case Chapter.GARTEN:
                    storypointCounter[3] = value;
                    break;

                case Chapter.GARTEN_INVERSE:
                    storypointCounter[4] = value;
                    break;

                case Chapter.TAUBENSCHLAG:
                    storypointCounter[6] = value;
                    break;

                case Chapter.PIGEON:
                    storypointCounter[8] = value;
                    break;

                case Chapter.TRICKSTER:
                    storypointCounter[10] = value;
                    break;

                default:
                    Debug.LogWarning("Storypoint counter override for invalid chapter: " + chapter);
                    break;
            }
        }

        public static int GetStorypointCounter(Chapter chapter)
        {
            //This returns how many StoryPoints are needed to end a chapter
            switch (chapter)
            {
                case Chapter.GARTEN:
                    return storypointCounter[3];

                case Chapter.GARTEN_INVERSE:
                    return storypointCounter[4];

                case Chapter.TAUBENSCHLAG:
                    return storypointCounter[6];

                case Chapter.PIGEON:
                    return storypointCounter[8];

                case Chapter.TRICKSTER:
                    return storypointCounter[10];

                default:
                    Debug.LogWarning("Storypoint counter requested for invalid chapter: " + chapter);
                    return -1;
            }



        }

        public static int GetStorypointMax(Chapter chapter)
        {
            //This returns how many StoryPoints can be found in a chapter
            switch (chapter)
            {
                case Chapter.NICHTS:
                    return 5;

                case Chapter.GARTEN:
                    return 10;

                case Chapter.GARTEN_INVERSE:
                    return 3;

                case Chapter.TAUBENSCHLAG:
                    return 7;

                case Chapter.PIGEON:
                    return 6;

                case Chapter.TRICKSTER:
                    return 5;

                default:
                    Debug.LogWarning("Storypoint counter requested for invalid chapter: " + chapter);
                    return -1;
            }
        }

    }
}


