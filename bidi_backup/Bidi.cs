using System;
using System.Text;

public class Bidi
{
    public string Convert(string str)
    {
        PrintWriter writer = new PrintWriter();
        BidiReferenceTestCharmap charmap = BidiReferenceTestCharmap.TEST_PBA;
        sbyte baseDirection = BidiReference.implicitEmbeddingLevel;
        byte doPBATest = 1;

        try
        {
            sbyte[] codes = charmap.getCodes(str);
            BidiTestBracketMap map = BidiTestBracketMap.TEST_BRACKETS;
            sbyte[] pbTypes = map.getBracketTypes(str);
            int[] pbValues = map.getBracketValues(str);

            BidiReference bidi = new BidiReference(codes, pbTypes, pbValues, baseDirection);
            int[] reorder = bidi.getReordering(new int[] { codes.Length });

            writer.println("base level: " + bidi.getBaseLevel() + (baseDirection != BidiReference.implicitEmbeddingLevel ? " (forced)" : ""));

            var sbInput = new StringBuilder(str.Length);
            var sbOutput = new StringBuilder(str.Length);

            // output original text
            for (int i = 0; i < str.Length; ++i)
            {
                sbInput.Append(str[i]);
            }
            writer.println();

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

            var input = sbInput.ToString();
            var output = sbOutput.ToString();

            return output;
        }
        catch (Exception e)
        {
            return e.ToString();
        }
    }
}