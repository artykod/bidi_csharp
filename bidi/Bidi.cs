using System;
using System.Collections.Generic;
using System.Text;

public class Bidi
{
    public string Convert(string str)
    {
        PrintWriter writer = new PrintWriter();
        BidiReferenceTestCharmap charmap = BidiReferenceTestCharmap.TEST_MIXED_PBA;
        sbyte baseDirection = BidiReference.implicitEmbeddingLevel;
        byte doPBATest = 1;

        try
        {
            sbyte[] codes = charmap.getCodes(str);
            BidiTestBracketMap map = BidiTestBracketMap.TEST_BRACKETS;
            sbyte[] pbTypes = map.getBracketTypes(str);
            int[] pbValues = map.getBracketValues(str);

            BidiReference bidi = new BidiReference(codes, pbTypes, pbValues, baseDirection);

            var breaksSet = new HashSet<int> { codes.Length };

            for (int i = 0; i < str.Length; ++i)
            {
                if (str[i] == '\n')
                {
                    if (i > 0)
                    {
                        breaksSet.Add(str[i - 1] == '\r' ? i - 1 : i);
                    }
                    breaksSet.Add(i + 1);
                }
            }

            var breaksList = new List<int>(breaksSet);
            breaksList.Sort();

            int[] reorder = bidi.getReordering(breaksList.ToArray());

            writer.println("base level: " + bidi.getBaseLevel() + (baseDirection != BidiReference.implicitEmbeddingLevel ? " (forced)" : ""));

            var sbOutput = new StringBuilder(str.Length);

            if (doPBATest == 1)
            {
                // report on paired bracket algorithm
                writer.println();
                writer.println("bracket pairs at:\n" + bidi.pba.getPairPositionsString()); /*bidi.pba.pairPositions.toString()*/
                writer.println("(last isolated run sequence processed, in relative offsets)");
                writer.println();
                writer.print("resolved directional types: ");
                charmap.dumpCodes(writer, bidi.getResultTypes());
            }

            // output visually ordered text
            for (int i = 0; i < str.Length; ++i)
            {
                sbOutput.Append(str[reorder[i]]);
            }
            writer.println();

            var input = str;
            var output = sbOutput.ToString();

            return output;
        }
        catch (Exception e)
        {
            return e.ToString();
        }
    }
}