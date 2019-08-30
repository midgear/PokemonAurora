using UnityEngine;
using System;
using PokemonHeader;
using System.Collections.Generic;

public static class FileParsing
{
    #region PublicInterface
    /// <summary>
    /// Returns the float corresponding to the given string.
    /// </summary>
    public static float FloatFromString(string str)
    {
        float foo;
        if (float.TryParse(str, out foo))
            return foo;
        else
            return 0.0f;
    }

    /// <summary>
    /// Returns the unsigned integer corresponding to the given string.
    /// </summary>
    public static uint UnsignedIntFromString(string str)
    {
        uint foo;
        if (uint.TryParse(str, out foo))
            return foo;
        else
            return 0;
    }

    /// <summary>
    /// Returns the signed integer corresponding to the given string.
    /// </summary>
    public static int IntFromString(string str)
    {
        int foo;
        if (int.TryParse(str, out foo))
            return foo;
        else
            return 0;
    }

    /// <summary>
    /// This function will take in a string and remove all whitespace before and after 
    /// the main 'message': Ex) "   hello world!  " ---> "hello world!"
    /// </summary>
    public static string StringTrimWhitespace(string str)
    {
        return str.Trim();
    }

    /// <summary>
    /// This function logs the set of ASCII characters that make up str. Each character is shown in 
    /// hexidecimal format.
    /// </summary>
    public static void DebugLogStringAsHexSet(string str)
    {
        char[] strArr = str.ToCharArray();
        string runningStr = "{";

        for (int i = 0; i < str.Length; i++)
        {
            int val = Convert.ToInt32(strArr[i]);
            runningStr += "0x" + val.ToString("X") + ", ";
        }
        runningStr += "}";
        Debug.Log(runningStr);
    }

    public static void DebugLogList<T>(List<T> awesomeList)
    {
        string message = "{\n";

        for (int i = 0; i < awesomeList.Count; i++)
        {
            message += awesomeList[i] + ",\n";
        }

        message += "}";
        Debug.Log(message);
    }

    // TODO(BluCloos): Surely, there is an easier way to do this...
    public static pokemon_type PokemonTypeFromString(string typeName)
    {
        switch (typeName.ToLower())
        {
            case "normal":
                return pokemon_type.NORMAL;
            case "fighting":
                return pokemon_type.FIGHTING;
            case "flying":
                return pokemon_type.FLYING;
            case "poison":
                return pokemon_type.POISON;
            case "ground":
                return pokemon_type.GROUND;
            case "rock":
                return pokemon_type.ROCK;
            case "bug":
                return pokemon_type.BUG;
            case "ghost":
                return pokemon_type.GHOST;
            case "steel":
                return pokemon_type.STEEL;
            case "fire":
                return pokemon_type.FIRE;
            case "water":
                return pokemon_type.WATER;
            case "grass":
                return pokemon_type.GRASS;
            case "electric":
                return pokemon_type.ELECTRIC;
            case "psychic":
                return pokemon_type.PSYCHIC;
            case "ice":
                return pokemon_type.ICE;
            case "dragon":
                return pokemon_type.DRAGON;
            case "dark":
                return pokemon_type.DARK;
            case "fairy":
                return pokemon_type.FAIRY;
            default:
                return pokemon_type.UNKNOWN;
        }
    }

    /// <summary>
    /// This function will take in a string corresponding to a line of text and do the following.
    /// BTW, don't pick '#' as the split character. It just won't work dude.
    /// 1. Ignore all comments on the line
    /// 2. Split the line at the splitCharacter, but don't add empty strings (adjacent splitCharacters!)
    /// 3. Clean up the elements of the split by trimming the whitespace.
    /// </summary>
    public static List<string> CleanLineSplit(string rawLine, char splitChar)
    {
        List<string> elements = new List<string>();

        int start = 0;
        int end = 0;

        bool inComment = false;
        for (int i = 0; i < rawLine.Length; i++)
        {
            if (!inComment)
            {
                char cChar = rawLine[i];
                
                if (cChar == '#')
                {
                    inComment = true;
                }
                else
                {
                    end = i + 1;
                }

                if (cChar == splitChar)
                {
                    string newElem = rawLine.Substring(start, end - start - 1);
                    newElem = StringTrimWhitespace(newElem);
                    if (newElem.Length != 0)
                        elements.Add(newElem);
                    start = end;
                }
            }
        }
        
        // Add the trailing stuff
        if (start != end)
        {
            string newElem = rawLine.Substring(start, end - start);
            newElem = StringTrimWhitespace(newElem);
            if (newElem.Length != 0)
                elements.Add(newElem);
        }

        return elements;
    }

    /// <summary>
    /// This function loads the resource file called resourceName and parses out the
    /// data into a pokemon_data structure.If the pokemon resource does exist, this function will
    /// return null. For pokemon whos data is not yet defined, their table entry is a null
    /// reference. This is for data saving purposes.
    /// </summary>
    public static PokemonData ParsePokemonDataResource(string resourceName, float defaultWalkingSpeed, float defaultRunningSpeed)
    {
        var textFile = Resources.Load<TextAsset>(resourceName);
        if (textFile != null)
        {
            string[] lines = textFile.text.Split('\n');
            bool inMoves = false;

            PokemonData pokemonData = new PokemonData();
            pokemonData.moves = new List<pokemon_move_meta>();
            pokemonData.walkingSpeed = defaultWalkingSpeed;
            pokemonData.runningSpeed = defaultRunningSpeed;

            for (uint i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (!inMoves)
                {
                    List<string> lineSplit = CleanLineSplit(line, ':');
                    string handle = lineSplit[0].ToLower();
                    if (lineSplit.Count > 1)
                    {
                        string content = lineSplit[1];

                        switch (handle)
                        {
                            case "name":
                                pokemonData.name = content;
                                break;
                            case "type1":
                                pokemonData.type1 = PokemonTypeFromString(content);
                                break;
                            case "type2":
                                pokemonData.type2 = PokemonTypeFromString(content);
                                break;
                            case "height":
                                pokemonData.height = FloatFromString(content);
                                break;
                            case "weight":
                                pokemonData.weight = FloatFromString(content);
                                break;
                            case "walkingspeed":
                                pokemonData.walkingSpeed = FloatFromString(content);
                                break;
                            case "runningspeed":
                                pokemonData.runningSpeed = FloatFromString(content);
                                break;
                            case "hp":
                                pokemonData.hpBaseStat = (byte)UnsignedIntFromString(content);
                                break;
                            case "attack":
                                pokemonData.attackBaseStat = (byte)UnsignedIntFromString(content);
                                break;
                            case "defense":
                                pokemonData.defenseBaseStat = (byte)UnsignedIntFromString(content);
                                break;
                            case "spattack":
                                pokemonData.spAttackBaseStat = (byte)UnsignedIntFromString(content);
                                break;
                            case "spdefense":
                                pokemonData.spDefenseBaseStat = (byte)UnsignedIntFromString(content);
                                break;
                            case "speed":
                                pokemonData.speedBaseStat = (byte)UnsignedIntFromString(content);
                                break;
                            case "levelingtype":
                                pokemonData.levelingType = content;
                                break;
                            case "catchrate":
                                pokemonData.catchRate = (byte)UnsignedIntFromString(content);
                                break;
                        }
                    }
                    else if (handle.Equals("moves"))
                    {
                        inMoves = true;
                    }
                }
                else
                {
                    pokemon_move_meta moveMeta;
                    moveMeta.levelLearnedAt = 0;

                    List<string> lineSplit = CleanLineSplit(line, '/');
                    moveMeta.moveName = lineSplit[0];
                    string level = lineSplit[1];

                    if (!level.Equals("-"))
                    {
                        moveMeta.levelLearnedAt = (byte)UnsignedIntFromString(level);
                    }

                    pokemonData.moves.Add(moveMeta);
                }
            }

            return pokemonData;
        }
        else
        {
            return null;
        }
    }

    public static PokemonMoveData ParsePokemonMoveDataResource(string resourceName)
    {
        var textFile = Resources.Load<TextAsset>(resourceName);
        if (textFile != null)
        {
            string[] lines = textFile.text.Split('\n');
            PokemonMoveData moveData = new PokemonMoveData();
            for (int i = 0; i < lines.Length; i++)
            {
                // TODO(BluCloos): This code here is very similar to the fucking
                // other code in the other parse function. This should be abstracted because
                // that's what we programmers do! Fuck yeah!
                string line = lines[i];
                List<string> lineSplit = CleanLineSplit(line, ':');
                string handle = lineSplit[0];
                string content = lineSplit[1];

                switch (handle.ToLower())
                {
                    case "name":
                        moveData.name = content;
                        break;
                    case "pp":
                        moveData.powerPoints = (int)UnsignedIntFromString(content);
                        break;
                    case "power":
                        moveData.basePower = (int)UnsignedIntFromString(content);
                        break;
                    case "type":
                        moveData.type = PokemonTypeFromString(content);
                        break;
                    case "class":
                        switch (content.ToLower())
                        {
                            case "melee":
                                moveData.moveClass = pokemon_move_class.MELEE;
                                break;
                            case "aoe":
                                moveData.moveClass = pokemon_move_class.AOE;
                                break;
                            case "projectile":
                                moveData.moveClass = pokemon_move_class.PROJECTILE;
                                break;
                        }
                        break;
                }
            }

            return moveData;
        }
        else
        {
            return null;
        }
    }

    public static float[,] ParseTypeTableResource(string resourceName)
    {
        var textFile = Resources.Load<TextAsset>(resourceName);
        float[,] result = new float[(int)pokemon_type.Count, (int)pokemon_type.Count];

        if (textFile != null)
        {
            string[] lines = textFile.text.Split('\n');
            for (int i = 0; i < lines.Length && i < (int)pokemon_type.Count; i++)
            {
                string line = lines[i];
                List<string> lineSplit = CleanLineSplit(line, ' ');
                for (int j = 0; j < lineSplit.Count && j < (int)pokemon_type.Count; j++)
                {
                    //Debug.Log(lineSplit[j]);
                    float mod = FloatFromString(lineSplit[j]);
                    result[i, j] = mod;
                }
            }

            // the unkown attacking type is basically non-existent, so uh, yah
            // yeah, this should do it.
            for (int i = 0; i < (int)pokemon_type.Count; i++)
            {
                result[(int)pokemon_type.UNKNOWN, i] = 1.0f;
                result[i, (int)pokemon_type.UNKNOWN] = 1.0f;
            }
        }
        else
        {
            // NOTE(Reader): In the case that we were unable to load the file just return the identity type table
            for (int x = 0; x < (int)pokemon_type.Count; x++)
            {
                for (int y = 0; y < (int)pokemon_type.Count; y++)
                {
                    result[y, x] = 1.0f;
                }
            }
        }

        return result;
    }

    public static void DebugTest()
    {
        // Test the clean line split against a myriad of test cases
        Debug.Log("Testing CleanLineSplit()");

        string testCase = "#Hello World!";
        Debug.Log("TestCase: CleanLineSplit(" + testCase + ", ' ')");
        DebugLogList<string>(CleanLineSplit(testCase, ' '));

        testCase = "            #Hello World!";
        Debug.Log("TestCase: CleanLineSplit(" + testCase + ", ':')");
        DebugLogList<string>(CleanLineSplit(testCase, ':'));

        testCase = "   1   0  1      4      0   # awesome comment!";
        Debug.Log("TestCase: CleanLineSplit(" + testCase + ", ' ')");
        DebugLogList<string>(CleanLineSplit(testCase, ' '));

        testCase = "   Name:     Pikachu    ";
        Debug.Log("TestCase: CleanLineSplit(" + testCase + ", ':')");
        DebugLogList<string>(CleanLineSplit(testCase, ':'));

        testCase = "Tackle/-";
        Debug.Log("TestCase: CleanLineSplit(" + testCase + ", '/')");
        DebugLogList<string>(CleanLineSplit(testCase, '/'));

        Debug.Log("End CleanLineSplit() test");
    }

    #endregion
}
