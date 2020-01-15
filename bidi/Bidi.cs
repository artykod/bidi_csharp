using System;
using System.Collections.Generic;
using System.Text;

public class Bidi
{
    private readonly bool _showDetails;

    public Bidi(bool showDetails = false)
    {
        _showDetails = showDetails;
    }

    public string Convert(string input)
    {
        if (!BidiApply(input, out _, out var visualToLogical, out _))
        {
            return input;
        }

        var output = new StringBuilder(input.Length);
        for (int i = 0; i < input.Length; ++i)
        {
            output.Append(input[visualToLogical[i]]);
        }
        return output.ToString();
    }

    public int LogicalToVisualCaretPosition(string input, int caret)
    {
        if (!BidiApply(input, out var directions, out _, out var logicalToVisual))
        {
            return caret;
        }

        if (caret <= 0 || input[caret - 1] == '\n') // TODO all linebreaks?
        {
            for (int i = caret; i < input.Length; ++i)
            {
                if (input[i] == '\n')
                {
                    return i;
                }
            }
            return input.Length;
        }

        if (caret >= input.Length || input[caret] == '\n') // TODO all linebreaks?
        {
            for (int i = caret - 1; i >= 0; --i)
            {
                if (input[i] == '\n')
                {
                    return i + 1;
                }
            }
            return 0;
        }

        return directions[caret] == BidiReference.R
            ? logicalToVisual[caret] + 1
            : directions[caret - 1] == BidiReference.R
                ? logicalToVisual[caret - 1]
                : logicalToVisual[caret];
    }

    public int VisualToLogicalCaretPosition(string input, int caret)
    {
        if (!BidiApply(input, out var directions, out var visualToLogical, out _))
        {
            return caret;
        }

        if (caret <= 0 || input[visualToLogical[caret - 1]] == '\n') // TODO all linebreaks?
        {
            for (int i = caret; i < input.Length; ++i)
            {
                if (input[i] == '\n')
                {
                    return i;
                }
            }
            return input.Length;
        }

        if (caret >= input.Length || input[visualToLogical[caret]] == '\n') // TODO all linebreaks?
        {
            for (int i = caret - 1; i >= 0; --i)
            {
                if (input[i] == '\n')
                {
                    return i + 1;
                }
            }
            return 0;
        }

        return directions[visualToLogical[caret]] == BidiReference.R
            ? visualToLogical[caret] + 1
            : directions[visualToLogical[caret - 1]] == BidiReference.R
                ? visualToLogical[caret - 1]
                : visualToLogical[caret];
    }

    public int ConvertSelectionPosition(string input, int selection)
    {
        if (!BidiApply(input, out _, out _, out var logicalToVisual))
        {
            return selection;
        }

        return logicalToVisual[selection];
    }

    private bool BidiApply(string input, out sbyte[] directions, out int[] visualToLogical, out int[] logicalToVisual)
    {
        directions = null;
        visualToLogical = null;
        logicalToVisual = null;

        try
        {
            var charmap = BidiReferenceTestCharmap.TEST_MIXED_PBA;
            var bracketmap = BidiTestBracketMap.TEST_BRACKETS;
            var baseDirection = (sbyte)1; // force RTL
            var chCodes = charmap.getCodes(input);
            var pbTypes = bracketmap.getBracketTypes(input);
            var pbValues = bracketmap.getBracketValues(input);

            // TODO add auto-linebreaks
            var linebreaksSet = new SortedSet<int> { chCodes.Length };
            for (int i = 0; i < input.Length; ++i)
            {
                if (input[i] == '\n')
                {
                    if (i > 0)
                    {
                        linebreaksSet.Add(input[i - 1] == '\r' ? i - 1 : i);
                    }
                    linebreaksSet.Add(i + 1);
                }
            }

            // TODO optimize Bidi algorithm
            var bidi = new BidiReference(chCodes, pbTypes, pbValues, baseDirection);
            directions = bidi.resultTypes;
            visualToLogical = bidi.getReordering(new List<int>(linebreaksSet).ToArray());
            logicalToVisual = new int[visualToLogical.Length];

            for (int i = 0; i < input.Length; ++i)
            {
                logicalToVisual[visualToLogical[i]] = i;
            }

            // TODO fix paired brackets inversion

            if (_showDetails)
            {
                var debugWriter = new PrintWriter();

                debugWriter.println("base level: " + bidi.getBaseLevel() + (baseDirection != BidiReference.implicitEmbeddingLevel ? " (forced)" : ""));
                debugWriter.println();

                // report on paired bracket algorithm
                debugWriter.println("bracket pairs at:\n" + bidi.pba.getPairPositionsString()); /*bidi.pba.pairPositions.toString()*/
                debugWriter.println("(last isolated run sequence processed, in relative offsets)");
                debugWriter.println();

                debugWriter.print("resolved directional types:\n");
                charmap.dumpCodes(debugWriter, bidi.getResultTypes());
                debugWriter.println();
            }

            return true;
        }
        catch (Exception e)
        {
            if (_showDetails)
            {
                new PrintWriter().println(e.ToString());
            }
            return false;
        }
    }
}