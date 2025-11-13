using UnityEngine;

namespace COLUMBARIUM.Global
{
    public enum Chapter
    {
        STARTSCREEN, //0
        PROLOG, //1
        NICHTS, //2
        GARTEN, //3
        GARTEN_ALTERNATIVE, //4
        TAUBENSCHLAG, //5
        PIDGEON, //6
        TRICKSTER, //7
        EMBRYO, //8
        FAREWELL, //9
        EPILOG, //10
        CREDITS //11
    }


        

    public static class GlobalProgress
    {
        public static string[] chapterNames = new string[12]
        {
            "STARTSCREEN",
            "PART 0: PROLOGUE",
            "PART I: NICHTS",
            "PART II: PARADISE GARDEN",
            "PART II.II: PARADISE GARDEN INVERSE",
            "PART III: DER TAUBENSCHLAG",
            "PART IV: BECOMING PIGEON",
            "PART V: BE A TRICKSTER, BUILD A WORLD",
            "PART VI: EMBRYO",
            "PART VII: FAREWELL",
            "PART VIII: EPILOGUE",
            "PART IX: CREDITS"
           
        };

        public static bool english = false;
        
        //This holds the amount of story points for each chapter
        private static int[] storypointCounter = new int[12]{0,0,0,5,3,7,6,5,0,0,0,0};

        public static void OverrideStorypointCounter(int index, int value) => storypointCounter[index] = value;
        public static int GetStorypointCounter(int index) => storypointCounter[index];
    }
    
    
}


