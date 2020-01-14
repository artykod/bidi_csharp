using System;
using System.Collections.Generic;
using System.Text;

public class Bidi
{
    private readonly bool showDetails;

    public Bidi(bool showDetails)
    {
        this.showDetails = showDetails;
    }

    public string Convert(string str)
    {
        var writer = new PrintWriter();
        var charmap = BidiReferenceTestCharmap.TEST_MIXED_PBA;
        var baseDirection = (sbyte)1; // force RTL

        try
        {
            var codes = charmap.getCodes(str);
            var map = BidiTestBracketMap.TEST_BRACKETS;
            var pbTypes = map.getBracketTypes(str);
            var pbValues = map.getBracketValues(str);
            var bidi = new BidiReference(codes, pbTypes, pbValues, baseDirection);

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

            var reorder = bidi.getReordering(breaksList.ToArray());

            // output visually ordered text
            var sbOutput = new StringBuilder(str.Length);
            for (int i = 0; i < str.Length; ++i)
            {
                sbOutput.Append(str[reorder[i]]);
            }

            if (showDetails)
            {
                writer.println("base level: " + bidi.getBaseLevel() + (baseDirection != BidiReference.implicitEmbeddingLevel ? " (forced)" : ""));
                writer.println();

                // report on paired bracket algorithm
                writer.println("bracket pairs at:\n" + bidi.pba.getPairPositionsString()); /*bidi.pba.pairPositions.toString()*/
                writer.println("(last isolated run sequence processed, in relative offsets)");
                writer.println();

                writer.print("resolved directional types:\n");
                charmap.dumpCodes(writer, bidi.getResultTypes());
                writer.println();
            }

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