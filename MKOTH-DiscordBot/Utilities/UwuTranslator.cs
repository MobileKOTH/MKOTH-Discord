using System;
using System.Collections.Generic;
using System.Text;

namespace MKOTHDiscordBot.Utilities
{
    public static class UwuTranslator
    {
        public static string Translate(string input)
        {
            var result = "";
            var previousChar = '\0';
            // Grab each character from the input to check
            // if a letter from the switch cases hit
            for (int i = 0; i < input.Length; i++)
            {

                // Variables for easy referencing
                char currentChar = input[i];

                // Switch cases for uwuness
                switch (currentChar)
                {
                    case 'L':
                    case 'R':
                        result += "W";
                        break;
                    case 'l':
                    case 'r':
                        result += "w";
                        break;

                    // This is a special case and it needs the
                    // previous letter for context.
                    case 'o':
                    case 'O':
                        // If it hits, then add the adorable "y"
                        // to the current letter "o"
                        switch (previousChar)
                        {
                            case 'n':
                            case 'N':
                            case 'm':
                            case 'M':
                                result += "yo";
                                break;
                            default:
                                result += currentChar;
                                break;
                        }
                        break;
                    default:
                        result += currentChar;
                        break;
                }

                previousChar = currentChar;
            }

            return result;
        }
    }
}
