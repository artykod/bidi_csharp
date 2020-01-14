/*
 * Last Revised: 2016-09-21
 *
 * Credits:
 * Originally written by Doug Felt
 * 
 * Updated for Unicode 6.3 by Roozbeh Pournader, with feedback by Aharon Lanin
 * 
 * Updated by Asmus Freytag to implement the Paired Bracket Algorithm (PBA)
 *
 * Updated for Unicode 8.0 by Deepak Jois, with feedback from Ken Whistler
 *
 * Disclaimer and legal rights:
 * (C) Copyright IBM Corp. 1999, All Rights Reserved
 * (C) Copyright Google Inc. 2013, All Rights Reserved
 * (C) Copyright ASMUS, Inc. 2013. All Rights Reserved
 * (C) Copyright Deepak Jois 2016, All Rights Reserved
 *
 * Distributed under the Terms of Use in http://www.unicode.org/copyright.html.
 */


/*
 * Revision info (2016-09-21):
 * Changes to support updated rules X5a,X5b and X6a in Unicode 8.0
 *
 * Revision info (2013-09-16):
 * Changed MAX_DEPTH to 125
 * 
 * Revision info (2013-06-02):
 * <p>
 * The core part of the Unicode Paired Bracket Algorithm (PBA) 
 * is implemented in a new BidiPBAReference class.
 * <p>
 * Changed convention for default paragraph embedding level from -1 to 2.
 */




using System;
/**
* Reference implementation of the Unicode Bidirectional Algorithm (UAX #9).
*
* <p>
* This implementation is not optimized for performance. It is intended as a
* reference implementation that closely follows the specification of the
* Bidirectional Algorithm in The Unicode Standard version 6.3.
* <p>
* <b>Input:</b><br>
* There are two levels of input to the algorithm, since clients may prefer to
* supply some information from out-of-band sources rather than relying on the
* default behavior.
* <ol>
* <li>Bidi class array
* <li>Bidi class array, with externally supplied base line direction
* </ol>
* <p>
* <b>Output:</b><br>
* Output is separated into several stages as well, to better enable clients to
* evaluate various aspects of implementation conformance.
* <ol>
* <li>levels array over entire paragraph
* <li>reordering array over entire paragraph
* <li>levels array over line
* <li>reordering array over line
* </ol>
* Note that for conformance to the Unicode Bidirectional Algorithm,
* implementations are only required to generate correct reordering and
* character directionality (odd or even levels) over a line. Generating
* identical level arrays over a line is not required. Bidi explicit format
* codes (LRE, RLE, LRO, RLO, PDF) and BN can be assigned arbitrary levels and
* positions as long as the rest of the input is properly reordered.
* <p>
* As the algorithm is defined to operate on a single paragraph at a time, this
* implementation is written to handle single paragraphs. Thus rule P1 is
* presumed by this implementation-- the data provided to the implementation is
* assumed to be a single paragraph, and either contains no 'B' codes, or a
* single 'B' code at the end of the input. 'B' is allowed as input to
* illustrate how the algorithm assigns it a level.
* <p>
* Also note that rules L3 and L4 depend on the rendering engine that uses the
* result of the bidi algorithm. This implementation assumes that the rendering
* engine expects combining marks in visual order (e.g. to the left of their
* base character in RTL runs) and that it adjusts the glyphs used to render
* mirrored characters that are in RTL runs so that they render appropriately.
*
* @author Doug Felt
* @author Roozbeh Pournader
* @author Asmus Freytag
* @author Deepak Jois
*/
public class BidiReference
{
    private readonly sbyte[] initialTypes;
    public const sbyte implicitEmbeddingLevel = 2; // level will be determined implicitly
    private sbyte paragraphEmbeddingLevel = implicitEmbeddingLevel;

    private int textLength; // for convenience
    private sbyte[] resultTypes; // for paragraph, not lines
    private sbyte[] resultLevels; // for paragraph, not lines

    public sbyte[] getResultTypes()
    {
        return (sbyte[])resultTypes.Clone();
    } // for display in test mode

    /*
     * Index of matching PDI for isolate initiator characters. For other
     * characters, the value of matchingPDI will be set to -1. For isolate
     * initiators with no matching PDI, matchingPDI will be set to the length of
     * the input string.
     */
    private int[] matchingPDI;

    /*
     * Index of matching isolate initiator for PDI characters. For other
     * characters, and for PDIs with no matching isolate initiator, the value of
     * matchingIsolateInitiator will be set to -1.
     */
    private int[] matchingIsolateInitiator;

    /*
     * Arrays of properties needed for paired bracket evaluation in N0
     */
    private readonly sbyte[] pairTypes; // paired Bracket types for paragraph
    private readonly int[] pairValues; // paired Bracket values for paragraph

    public BidiPBAReference pba; // to allow access to internal pba state for diagnostics

    // The bidi types

    /** Left-to-right */
    public const sbyte L = 0;

    /** Left-to-Right Embedding */
    public const sbyte LRE = 1;

    /** Left-to-Right Override */
    public const sbyte LRO = 2;

    /** Right-to-Left */
    public const sbyte R = 3;

    /** Right-to-Left Arabic */
    public const sbyte AL = 4;

    /** Right-to-Left Embedding */
    public const sbyte RLE = 5;

    /** Right-to-Left Override */
    public const sbyte RLO = 6;

    /** Pop Directional Format */
    public const sbyte PDF = 7;

    /** European Number */
    public const sbyte EN = 8;

    /** European Number Separator */
    public const sbyte ES = 9;

    /** European Number Terminator */
    public const sbyte ET = 10;

    /** Arabic Number */
    public const sbyte AN = 11;

    /** Common Number Separator */
    public const sbyte CS = 12;

    /** Non-Spacing Mark */
    public const sbyte NSM = 13;

    /** Boundary Neutral */
    public const sbyte BN = 14;

    /** Paragraph Separator */
    public const sbyte B = 15;

    /** Segment Separator */
    public const sbyte S = 16;

    /** Whitespace */
    public const sbyte WS = 17;

    /** Other Neutrals */
    public const sbyte ON = 18;

    /** Left-to-Right Isolate */
    public const sbyte LRI = 19;

    /** Right-to-Left Isolate */
    public const sbyte RLI = 20;

    /** First-Strong Isolate */
    public const sbyte FSI = 21;

    /** Pop Directional Isolate */
    public const sbyte PDI = 22;

    /** Minimum bidi type value. */
    public const sbyte TYPE_MIN = 0;

    /** Maximum bidi type value. */
    public const sbyte TYPE_MAX = 22;

    /** Shorthand names of bidi type values, for error reporting. */
    public static readonly string[] typenames = {
            "L",
            "LRE",
            "LRO",
            "R",
            "AL",
            "RLE",
            "RLO",
            "PDF",
            "EN",
            "ES",
            "ET",
            "AN",
            "CS",
            "NSM",
            "BN",
            "B",
            "S",
            "WS",
            "ON",
            "LRI",
            "RLI",
            "FSI",
            "PDI"
    };

    //
    // Input
    //

    /**
     * Initialize using several arrays, then run the algorithm
     * @param types
     *            Array of types ranging from TYPE_MIN to TYPE_MAX inclusive 
     *            and representing the direction codes of the characters in the text.
     * @param pairTypes
     * 			  Array of paired bracket types ranging from 0 (none) to 2 (closing)
     * 			  of the characters
     * @param pairValues
     * 			  Array identifying which set of matching bracket characters
     * 			  as defined in BidiPBAReference (note, both opening and closing
     * 			  bracket get the same value if they are part of the same canonical "set"
     * 			  or pair)
     */
    public BidiReference(sbyte[] types, sbyte[] pairTypes, int[] pairValues)
    {
        validateTypes(types);
        validatePbTypes(pairTypes);
        validatePbValues(pairValues, pairTypes);

        initialTypes = (sbyte[])types.Clone(); // client type array remains unchanged
        this.pairTypes = pairTypes;
        this.pairValues = pairValues;

        runAlgorithm();
    }

    /**
     * Initialize using several arrays of direction and other types and an externally supplied
     * paragraph embedding level. The embedding level may be  0, 1 or 2.
     * <p>
     * 2 means to apply the default algorithm (rules P2 and P3), 0 is for LTR
     * paragraphs, and 1 is for RTL paragraphs.
     *
     * @param types
     *            the types array
     * @param pairTypes
     *           the paired bracket types array
     * @param pairValues
     * 			 the paired bracket values array
     * @param paragraphEmbeddingLevel
     *            the externally supplied paragraph embedding level.
     */
    public BidiReference(sbyte[] types, sbyte[] pairTypes, int[] pairValues, sbyte paragraphEmbeddingLevel)
    {
        validateTypes(types);
        validatePbTypes(pairTypes);
        validatePbValues(pairValues, pairTypes);
        validateParagraphEmbeddingLevel(paragraphEmbeddingLevel);

        initialTypes = (sbyte[])types.Clone(); // client type array remains unchanged
        this.paragraphEmbeddingLevel = paragraphEmbeddingLevel;
        this.pairTypes = pairTypes;
        this.pairValues = pairValues;

        runAlgorithm();
    }

    /**
     * The algorithm. Does not include line-based processing (Rules L1, L2).
     * These are applied later in the line-based phase of the algorithm.
     */
    private void runAlgorithm()
    {
        textLength = initialTypes.Length;

        // Initialize output types.
        // Result types initialized to input types.
        resultTypes = (sbyte[])initialTypes.Clone();

        // Preprocessing to find the matching isolates
        determineMatchingIsolates();

        // 1) determining the paragraph level
        // Rule P1 is the requirement for entering this algorithm.
        // Rules P2, P3.
        // If no externally supplied paragraph embedding level, use default.
        if (paragraphEmbeddingLevel == implicitEmbeddingLevel)
        {
            paragraphEmbeddingLevel = determineParagraphEmbeddingLevel(0, textLength);
        }

        // Initialize result levels to paragraph embedding level.
        resultLevels = new sbyte[textLength];
        setLevels(resultLevels, 0, textLength, paragraphEmbeddingLevel);

        // 2) Explicit levels and directions
        // Rules X1-X8.
        determineExplicitEmbeddingLevels();

        // Rule X9.
        // We do not remove the embeddings, the overrides, the PDFs, and the BNs
        // from the string explicitly. But they are not copied into isolating run
        // sequences when they are created, so they are removed for all
        // practical purposes.

        // Rule X10.
        // Run remainder of algorithm one isolating run sequence at a time
        IsolatingRunSequence[] sequences = determineIsolatingRunSequences();

        for (int i = 0; i < sequences.Length; ++i)
        {
            IsolatingRunSequence sequence = sequences[i];
            // 3) resolving weak types
            // Rules W1-W7.
            sequence.resolveWeakTypes();

            // 4a) resolving paired brackets
            // Rule N0
            pba = sequence.resolvePairedBrackets();

            // 4b) resolving neutral types
            // Rules N1-N3.
            sequence.resolveNeutralTypes();

            // 5) resolving implicit embedding levels
            // Rules I1, I2.
            sequence.resolveImplicitLevels();

            // Apply the computed levels and types
            sequence.applyLevelsAndTypes();
        }

        // Assign appropriate levels to 'hide' LREs, RLEs, LROs, RLOs, PDFs, and
        // BNs. This is for convenience, so the resulting level array will have
        // a value for every character.
        assignLevelsToCharactersRemovedByX9();
    }

    /**
     * Determine the matching PDI for each isolate initiator and vice versa.
     * <p>
     * Definition BD9.
     * <p>
     * At the end of this function:
     * <ul>
     * <li>The member variable matchingPDI is set to point to the index of the
     * matching PDI character for each isolate initiator character. If there is
     * no matching PDI, it is set to the length of the input text. For other
     * characters, it is set to -1.
     * <li>The member variable matchingIsolateInitiator is set to point to the
     * index of the matching isolate initiator character for each PDI character.
     * If there is no matching isolate initiator, or the character is not a PDI,
     * it is set to -1.
     * </ul>
     */
    private void determineMatchingIsolates()
    {
        matchingPDI = new int[textLength];
        matchingIsolateInitiator = new int[textLength];

        for (int i = 0; i < textLength; ++i)
        {
            matchingIsolateInitiator[i] = -1;
        }

        for (int i = 0; i < textLength; ++i)
        {
            matchingPDI[i] = -1;

            sbyte t = resultTypes[i];
            if (t == LRI || t == RLI || t == FSI)
            {
                int depthCounter = 1;
                for (int j = i + 1; j < textLength; ++j)
                {
                    sbyte u = resultTypes[j];
                    if (u == LRI || u == RLI || u == FSI)
                    {
                        ++depthCounter;
                    }
                    else if (u == PDI)
                    {
                        --depthCounter;
                        if (depthCounter == 0)
                        {
                            matchingPDI[i] = j;
                            matchingIsolateInitiator[j] = i;
                            break;
                        }
                    }
                }
                if (matchingPDI[i] == -1)
                {
                    matchingPDI[i] = textLength;
                }
            }
        }
    }

    /**
     * Determines the paragraph level based on rules P2, P3. This is also used
     * in rule X5c to find if an FSI should resolve to LRI or RLI.
     *
     * @param startIndex
     *            the index of the beginning of the substring
     * @param endIndex
     *            the index of the character after the end of the string
     *
     * @return the resolved paragraph direction of the substring limited by
     *         startIndex and endIndex
     */
    private sbyte determineParagraphEmbeddingLevel(int startIndex, int endIndex)
    {
        sbyte strongType = -1; // unknown

        // Rule P2.
        for (int i = startIndex; i < endIndex; ++i)
        {
            sbyte t = resultTypes[i];
            if (t == L || t == AL || t == R)
            {
                strongType = t;
                break;
            }
            else if (t == FSI || t == LRI || t == RLI)
            {
                i = matchingPDI[i]; // skip over to the matching PDI
                //assert(i <= endIndex);
            }
        }

        // Rule P3.
        if (strongType == -1)
        { // none found
            // default embedding level when no strong types found is 0.
            return 0;
        }
        else if (strongType == L)
        {
            return 0;
        }
        else
        { // AL, R
            return 1;
        }
    }

    public const int MAX_DEPTH = 125;

    // This stack will store the embedding levels and override and isolated
    // statuses
    private class directionalStatusStack
    {
        private int stackCounter = 0;
        private readonly sbyte[] embeddingLevelStack = new sbyte[MAX_DEPTH + 1];
        private readonly sbyte[] overrideStatusStack = new sbyte[MAX_DEPTH + 1];
        private readonly bool[] isolateStatusStack = new bool[MAX_DEPTH + 1];

        public void empty()
        {
            stackCounter = 0;
        }

        public void push(sbyte level, sbyte overrideStatus, bool isolateStatus)
        {
            embeddingLevelStack[stackCounter] = level;
            overrideStatusStack[stackCounter] = overrideStatus;
            isolateStatusStack[stackCounter] = isolateStatus;
            ++stackCounter;
        }

        public void pop()
        {
            --stackCounter;
        }

        public int depth()
        {
            return stackCounter;
        }

        public sbyte lastEmbeddingLevel()
        {
            return embeddingLevelStack[stackCounter - 1];
        }

        public sbyte lastDirectionalOverrideStatus()
        {
            return overrideStatusStack[stackCounter - 1];
        }

        public bool lastDirectionalIsolateStatus()
        {
            return isolateStatusStack[stackCounter - 1];
        }
    }

    /**
     * Determine explicit levels using rules X1 - X8
     */
    private void determineExplicitEmbeddingLevels()
    {
        directionalStatusStack stack = new directionalStatusStack();
        int overflowIsolateCount, overflowEmbeddingCount, validIsolateCount;

        // Rule X1.
        stack.empty();
        stack.push(paragraphEmbeddingLevel, ON, false);
        overflowIsolateCount = 0;
        overflowEmbeddingCount = 0;
        validIsolateCount = 0;
        for (int i = 0; i < textLength; ++i)
        {
            sbyte t = resultTypes[i];

            // Rules X2, X3, X4, X5, X5a, X5b, X5c
            switch (t)
            {
                case RLE:
                case LRE:
                case RLO:
                case LRO:
                case RLI:
                case LRI:
                case FSI:
                    bool isIsolate = (t == RLI || t == LRI || t == FSI);
                    bool isRTL = (t == RLE || t == RLO || t == RLI);
                    // override if this is an FSI that resolves to RLI
                    if (t == FSI)
                    {
                        isRTL = (determineParagraphEmbeddingLevel(i + 1, matchingPDI[i]) == 1);
                    }

                    if (isIsolate)
                    {
                        resultLevels[i] = stack.lastEmbeddingLevel();
                        if (stack.lastDirectionalOverrideStatus() != ON)
                        {
                            resultTypes[i] = stack.lastDirectionalOverrideStatus();
                        }
                    }

                    sbyte newLevel;
                    if (isRTL)
                    {
                        // least greater odd
                        newLevel = (sbyte)((stack.lastEmbeddingLevel() + 1) | 1);
                    }
                    else
                    {
                        // least greater even
                        newLevel = (sbyte)((stack.lastEmbeddingLevel() + 2) & ~1);
                    }

                    if (newLevel <= MAX_DEPTH && overflowIsolateCount == 0 && overflowEmbeddingCount == 0)
                    {
                        if (isIsolate)
                        {
                            ++validIsolateCount;
                        }
                        // Push new embedding level, override status, and isolated
                        // status.
                        // No check for valid stack counter, since the level check
                        // suffices.
                        stack.push(
                                newLevel,
                                t == LRO ? L : t == RLO ? R : ON,
                                isIsolate);

                        // Not really part of the spec
                        if (!isIsolate)
                        {
                            resultLevels[i] = newLevel;
                        }
                    }
                    else
                    {
                        // This is an invalid explicit formatting character,
                        // so apply the "Otherwise" part of rules X2-X5b.
                        if (isIsolate)
                        {
                            ++overflowIsolateCount;
                        }
                        else
                        { // !isIsolate
                            if (overflowIsolateCount == 0)
                            {
                                ++overflowEmbeddingCount;
                            }
                        }
                    }
                    break;

                // Rule X6a
                case PDI:
                    if (overflowIsolateCount > 0)
                    {
                        --overflowIsolateCount;
                    }
                    else if (validIsolateCount == 0)
                    {
                        // do nothing
                    }
                    else
                    {
                        overflowEmbeddingCount = 0;
                        while (!stack.lastDirectionalIsolateStatus())
                        {
                            stack.pop();
                        }
                        stack.pop();
                        --validIsolateCount;
                    }
                    resultLevels[i] = stack.lastEmbeddingLevel();
                    break;

                // Rule X7
                case PDF:
                    // Not really part of the spec
                    resultLevels[i] = stack.lastEmbeddingLevel();

                    if (overflowIsolateCount > 0)
                    {
                        // do nothing
                    }
                    else if (overflowEmbeddingCount > 0)
                    {
                        --overflowEmbeddingCount;
                    }
                    else if (!stack.lastDirectionalIsolateStatus() && stack.depth() >= 2)
                    {
                        stack.pop();
                    }
                    else
                    {
                        // do nothing
                    }
                    break;

                case B:
                    // Rule X8.

                    // These values are reset for clarity, in this implementation B
                    // can only occur as the last code in the array.
                    stack.empty();
                    stack.push(paragraphEmbeddingLevel, ON, false);
                    overflowIsolateCount = 0;
                    overflowEmbeddingCount = 0;
                    validIsolateCount = 0;
                    resultLevels[i] = paragraphEmbeddingLevel;
                    break;

                default:
                    resultLevels[i] = stack.lastEmbeddingLevel();
                    if (stack.lastDirectionalOverrideStatus() != ON)
                    {
                        resultTypes[i] = stack.lastDirectionalOverrideStatus();
                    }
                    break;
            }
        }
    }

    private class IsolatingRunSequence
    {
        private readonly int[] indexes; // indexes to the original string
        private readonly sbyte[] types; // type of each character using the index
        private sbyte[] resolvedLevels; // resolved levels after application of
                                        // rules
        private readonly int length; // length of isolating run sequence in
                                  // characters
        private readonly sbyte level;
        private readonly sbyte sos, eos;

        // parent values
        private readonly int textLength;
        private readonly int[] pairValues;
        private readonly sbyte[] pairTypes;
        private readonly sbyte paragraphEmbeddingLevel;
        private readonly sbyte[] initialTypes;
        private readonly sbyte[] resultTypes;
        private readonly sbyte[] resultLevels;

        /**
         * Rule X10, second bullet: Determine the start-of-sequence (sos) and end-of-sequence (eos) types,
         * 			 either L or R, for each isolating run sequence.
         * @param inputIndexes
         */
        public IsolatingRunSequence(int[] inputIndexes, BidiReference bidi)
        {
            textLength = bidi.textLength;
            pairValues = bidi.pairValues;
            pairTypes = bidi.pairTypes;
            paragraphEmbeddingLevel = bidi.paragraphEmbeddingLevel;
            initialTypes = bidi.initialTypes;
            resultTypes = bidi.resultTypes;
            resultLevels = bidi.resultLevels;

            indexes = inputIndexes;
            length = indexes.Length;

            types = new sbyte[length];
            for (int i = 0; i < length; ++i)
            {
                types[i] = resultTypes[indexes[i]];
            }

            // assign level, sos and eos
            level = resultLevels[indexes[0]];

            int prevChar = indexes[0] - 1;
            while (prevChar >= 0 && isRemovedByX9(initialTypes[prevChar]))
            {
                --prevChar;
            }
            sbyte prevLevel = prevChar >= 0 ? resultLevels[prevChar] : paragraphEmbeddingLevel;
            sos = typeForLevel(Math.Max(prevLevel, level));

            sbyte lastType = types[length - 1];
            sbyte succLevel;
            if (lastType == LRI || lastType == RLI || lastType == FSI)
            {
                succLevel = paragraphEmbeddingLevel;
            }
            else
            {
                int limit = indexes[length - 1] + 1; // the first character
                                                     // after the end of
                                                     // run sequence
                while (limit < textLength && isRemovedByX9(initialTypes[limit]))
                {
                    ++limit;
                }
                succLevel = limit < textLength ? resultLevels[limit] : paragraphEmbeddingLevel;
            }
            eos = typeForLevel(Math.Max(succLevel, level));
        }

        /**
         * Resolving bidi paired brackets  Rule N0
         */

        public BidiPBAReference resolvePairedBrackets()
        {
            var pba = new BidiPBAReference();
            pba.resolvePairedBrackets(indexes, initialTypes, types, pairTypes, pairValues, sos, level);
            return pba;
        }


        /**
         * Resolving weak types Rules W1-W7.
         *
         * Note that some weak types (EN, AN) remain after this processing is
         * complete.
         */
        public void resolveWeakTypes()
        {

            // on entry, only these types remain
            assertOnly(new sbyte[] { L, R, AL, EN, ES, ET, AN, CS, B, S, WS, ON, NSM, LRI, RLI, FSI, PDI });

            // Rule W1.
            // Changes all NSMs.
            sbyte preceedingCharacterType = sos;
            for (int i = 0; i < length; ++i)
            {
                sbyte t = types[i];
                if (t == NSM)
                {
                    types[i] = preceedingCharacterType;
                }
                else
                {
                    if (t == LRI || t == RLI || t == FSI || t == PDI)
                    {
                        preceedingCharacterType = ON;
                    }
                    preceedingCharacterType = t;
                }
            }

            // Rule W2.
            // EN does not change at the start of the run, because sos != AL.
            for (int i = 0; i < length; ++i)
            {
                if (types[i] == EN)
                {
                    for (int j = i - 1; j >= 0; --j)
                    {
                        sbyte t = types[j];
                        if (t == L || t == R || t == AL)
                        {
                            if (t == AL)
                            {
                                types[i] = AN;
                            }
                            break;
                        }
                    }
                }
            }

            // Rule W3.
            for (int i = 0; i < length; ++i)
            {
                if (types[i] == AL)
                {
                    types[i] = R;
                }
            }

            // Rule W4.
            // Since there must be values on both sides for this rule to have an
            // effect, the scan skips the first and last value.
            //
            // Although the scan proceeds left to right, and changes the type
            // values in a way that would appear to affect the computations
            // later in the scan, there is actually no problem. A change in the
            // current value can only affect the value to its immediate right,
            // and only affect it if it is ES or CS. But the current value can
            // only change if the value to its right is not ES or CS. Thus
            // either the current value will not change, or its change will have
            // no effect on the remainder of the analysis.

            for (int i = 1; i < length - 1; ++i)
            {
                if (types[i] == ES || types[i] == CS)
                {
                    sbyte prevSepType = types[i - 1];
                    sbyte succSepType = types[i + 1];
                    if (prevSepType == EN && succSepType == EN)
                    {
                        types[i] = EN;
                    }
                    else if (types[i] == CS && prevSepType == AN && succSepType == AN)
                    {
                        types[i] = AN;
                    }
                }
            }

            // Rule W5.
            for (int i = 0; i < length; ++i)
            {
                if (types[i] == ET)
                {
                    // locate end of sequence
                    int runstart = i;
                    int runlimit = findRunLimit(runstart, length, new sbyte[] { ET });

                    // check values at ends of sequence
                    sbyte t = runstart == 0 ? sos : types[runstart - 1];

                    if (t != EN)
                    {
                        t = runlimit == length ? eos : types[runlimit];
                    }

                    if (t == EN)
                    {
                        setTypes(runstart, runlimit, EN);
                    }

                    // continue at end of sequence
                    i = runlimit;
                }
            }

            // Rule W6.
            for (int i = 0; i < length; ++i)
            {
                sbyte t = types[i];
                if (t == ES || t == ET || t == CS)
                {
                    types[i] = ON;
                }
            }

            // Rule W7.
            for (int i = 0; i < length; ++i)
            {
                if (types[i] == EN)
                {
                    // set default if we reach start of run
                    sbyte prevStrongType = sos;
                    for (int j = i - 1; j >= 0; --j)
                    {
                        sbyte t = types[j];
                        if (t == L || t == R)
                        { // AL's have been changed to R
                            prevStrongType = t;
                            break;
                        }
                    }
                    if (prevStrongType == L)
                    {
                        types[i] = L;
                    }
                }
            }
        }

        /**
         * 6) resolving neutral types Rules N1-N2.
         */
        public void resolveNeutralTypes()
        {

            // on entry, only these types can be in resultTypes
            assertOnly(new sbyte[] { L, R, EN, AN, B, S, WS, ON, RLI, LRI, FSI, PDI });

            for (int i = 0; i < length; ++i)
            {
                sbyte t = types[i];
                if (t == WS || t == ON || /*t == B || */t == S || t == RLI || t == LRI || t == FSI || t == PDI)
                {
                    // find bounds of run of neutrals
                    int runstart = i;
                    int runlimit = findRunLimit(runstart, length, new sbyte[] { /*B, */S, WS, ON, RLI, LRI, FSI, PDI });

                    // determine effective types at ends of run
                    sbyte leadingType;
                    sbyte trailingType;

                    // Note that the character found can only be L, R, AN, or
                    // EN.
                    if (runstart == 0)
                    {
                        leadingType = sos;
                    }
                    else
                    {
                        leadingType = types[runstart - 1];
                        if (leadingType == AN || leadingType == EN)
                        {
                            leadingType = R;
                        }
                    }

                    if (runlimit == length)
                    {
                        trailingType = eos;
                    }
                    else
                    {
                        trailingType = types[runlimit];
                        if (trailingType == AN || trailingType == EN)
                        {
                            trailingType = R;
                        }
                    }

                    sbyte resolvedType;
                    if (leadingType == trailingType)
                    {
                        // Rule N1.
                        resolvedType = leadingType;
                    }
                    else
                    {
                        // Rule N2.
                        // Notice the embedding level of the run is used, not
                        // the paragraph embedding level.
                        resolvedType = typeForLevel(level);
                    }

                    setTypes(runstart, runlimit, resolvedType);

                    // skip over run of (former) neutrals
                    i = runlimit;
                }
            }
        }

        /**
         * 7) resolving implicit embedding levels Rules I1, I2.
         */
        public void resolveImplicitLevels()
        {

            // on entry, only these types can be in resultTypes
            assertOnly(new sbyte[] { L, R, EN, AN });

            resolvedLevels = new sbyte[length];
            setLevels(resolvedLevels, 0, length, level);

            if ((level & 1) == 0)
            { // even level
                for (int i = 0; i < length; ++i)
                {
                    sbyte t = types[i];
                    // Rule I1.
                    if (t == L)
                    {
                        // no change
                    }
                    else if (t == R)
                    {
                        resolvedLevels[i] += 1;
                    }
                    else
                    { // t == AN || t == EN
                        resolvedLevels[i] += 2;
                    }
                }
            }
            else
            { // odd level
                for (int i = 0; i < length; ++i)
                {
                    sbyte t = types[i];
                    // Rule I2.
                    if (t == R)
                    {
                        // no change
                    }
                    else
                    { // t == L || t == AN || t == EN
                        resolvedLevels[i] += 1;
                    }
                }
            }
        }

        /*
         * Applies the levels and types resolved in rules W1-I2 to the
         * resultLevels array.
         */
        public void applyLevelsAndTypes()
        {
            for (int i = 0; i < length; ++i)
            {
                int originalIndex = indexes[i];
                resultTypes[originalIndex] = types[i];
                resultLevels[originalIndex] = resolvedLevels[i];
            }
        }

        /**
         * Return the limit of the run consisting only of the types in validSet
         * starting at index. This checks the value at index, and will return
         * index if that value is not in validSet.
         */
        private int findRunLimit(int index, int limit, sbyte[] validSet)
        {
            while (index < limit)
            {
                sbyte t = types[index];
                var continueLoop = false;

                for (int i = 0; i < validSet.Length; ++i)
                {
                    if (t == validSet[i])
                    {
                        ++index;
                        continueLoop = true;
                        break;
                    }
                }

                if (continueLoop)
                {
                    continue;
                }

                // didn't find a match in validSet
                return index;
            }
            return limit;
        }

        /**
         * Set types from start up to (but not including) limit to newType.
         */
        private void setTypes(int start, int limit, sbyte newType)
        {
            for (int i = start; i < limit; ++i)
            {
                types[i] = newType;
            }
        }

        /**
         * Algorithm validation. Assert that all values in types are in the
         * provided set.
         */
        private void assertOnly(sbyte[] codes)
        {
        /*loop:
            for (int i = 0; i < length; ++i)
            {
                sbyte t = types[i];
                for (int j = 0; j < codes.Length; ++j)
                {
                    if (t == codes[j])
                    {
                        continue loop;
                    }
                }

                throw new Error("invalid bidi code " + typenames[t] + " present in assertOnly at position " + indexes[i]);
            }*/
        }
    }

    private static class Arrays
    {
        public static T[] copyOf<T>(T[] source, int length)
        {
            var result = new T[length];
            Array.Copy(source, result, length);
            return result;
        }
    }

    /**
     * Determines the level runs. Rule X9 will be applied in determining the
     * runs, in the way that makes sure the characters that are supposed to be
     * removed are not included in the runs.
     *
     * @return an array of level runs. Each level run is described as an array
     *         of indexes into the input string.
     */
    private int[][] determineLevelRuns()
    {
        // temporary array to hold the run
        int[] temporaryRun = new int[textLength];
        // temporary array to hold the list of runs
        int[][] allRuns = new int[textLength][];
        int numRuns = 0;

        sbyte currentLevel = (sbyte)-1;
        int runLength = 0;
        for (int i = 0; i < textLength; ++i)
        {
            if (!isRemovedByX9(initialTypes[i]))
            {
                if (resultLevels[i] != currentLevel)
                { // we just encountered a
                  // new run
                  // Wrap up last run
                    if (currentLevel >= 0)
                    { // only wrap it up if there was a run
                        int[] run = Arrays.copyOf(temporaryRun, runLength);
                        allRuns[numRuns] = run;
                        ++numRuns;
                    }
                    // Start new run
                    currentLevel = resultLevels[i];
                    runLength = 0;
                }
                temporaryRun[runLength] = i;
                ++runLength;
            }
        }
        // Wrap up the final run, if any
        if (runLength != 0)
        {
            int[] run = Arrays.copyOf(temporaryRun, runLength);
            allRuns[numRuns] = run;
            ++numRuns;
        }

        return Arrays.copyOf(allRuns, numRuns);
    }

    /**
	 * Definition BD13. Determine isolating run sequences.
	 *
	 * @return an array of isolating run sequences.
	 */
    private IsolatingRunSequence[] determineIsolatingRunSequences()
    {
        int[][] levelRuns = determineLevelRuns();
        int numRuns = levelRuns.Length;

        // Compute the run that each character belongs to
        int[] runForCharacter = new int[textLength];
        for (int runNumber = 0; runNumber < numRuns; ++runNumber)
        {
            for (int i = 0; i < levelRuns[runNumber].Length; ++i)
            {
                int characterIndex = levelRuns[runNumber][i];
                runForCharacter[characterIndex] = runNumber;
            }
        }

        IsolatingRunSequence[] sequences = new IsolatingRunSequence[numRuns];
        int numSequences = 0;
        int[] currentRunSequence = new int[textLength];
        for (int i = 0; i < levelRuns.Length; ++i)
        {
            int firstCharacter = levelRuns[i][0];
            if (initialTypes[firstCharacter] != PDI || matchingIsolateInitiator[firstCharacter] == -1)
            {
                int currentRunSequenceLength = 0;
                int run = i;
                do
                {
                    // Copy this level run into currentRunSequence
                    Array.Copy(levelRuns[run], 0, currentRunSequence, currentRunSequenceLength, levelRuns[run].Length);
                    currentRunSequenceLength += levelRuns[run].Length;

                    int lastCharacter = currentRunSequence[currentRunSequenceLength - 1];
                    sbyte lastType = initialTypes[lastCharacter];
                    if ((lastType == LRI || lastType == RLI || lastType == FSI) &&
                            matchingPDI[lastCharacter] != textLength)
                    {
                        run = runForCharacter[matchingPDI[lastCharacter]];
                    }
                    else
                    {
                        break;
                    }
                } while (true);

                sequences[numSequences] = new IsolatingRunSequence(Arrays.copyOf(currentRunSequence, currentRunSequenceLength), this);
                ++numSequences;
            }
        }
        return Arrays.copyOf(sequences, numSequences);
    }

    /**
     * Assign level information to characters removed by rule X9. This is for
     * ease of relating the level information to the original input data. Note
     * that the levels assigned to these codes are arbitrary, they're chosen so
     * as to avoid breaking level runs.
     *
     * @return the length of the data (original length of types array supplied
     *         to constructor)
     */
    private int assignLevelsToCharactersRemovedByX9()
    {
        for (int i = 0; i < initialTypes.Length; ++i)
        {
            sbyte t = initialTypes[i];
            if (t == LRE || t == RLE || t == LRO || t == RLO || t == PDF || t == BN)
            {
                resultTypes[i] = t;
                resultLevels[i] = -1;
            }
        }

        // now propagate forward the levels information (could have
        // propagated backward, the main thing is not to introduce a level
        // break where one doesn't already exist).

        if (resultLevels[0] == -1)
        {
            resultLevels[0] = paragraphEmbeddingLevel;
        }
        for (int i = 1; i < initialTypes.Length; ++i)
        {
            if (resultLevels[i] == -1)
            {
                resultLevels[i] = resultLevels[i - 1];
            }
        }

        // Embedding information is for informational purposes only
        // so need not be adjusted.

        return initialTypes.Length;
    }

    //
    // Output
    //

    /**
     * Return levels array breaking lines at offsets in linebreaks. <br>
     * Rule L1.
     * <p>
     * The returned levels array contains the resolved level for each bidi code
     * passed to the constructor.
     * <p>
     * The linebreaks array must include at least one value. The values must be
     * in strictly increasing order (no duplicates) between 1 and the length of
     * the text, inclusive. The last value must be the length of the text.
     *
     * @param linebreaks
     *            the offsets at which to break the paragraph
     * @return the resolved levels of the text
     */
    public sbyte[] getLevels(int[] linebreaks)
    {

        // Note that since the previous processing has removed all
        // P, S, and WS values from resultTypes, the values referred to
        // in these rules are the initial types, before any processing
        // has been applied (including processing of overrides).
        //
        // This example implementation has reinserted explicit format codes
        // and BN, in order that the levels array correspond to the
        // initial text. Their final placement is not normative.
        // These codes are treated like WS in this implementation,
        // so they don't interrupt sequences of WS.

        validateLineBreaks(linebreaks, textLength);

        sbyte[] result = (sbyte[])resultLevels.Clone(); // will be returned to
                                               // caller

        // don't worry about linebreaks since if there is a break within
        // a series of WS values preceding S, the linebreak itself
        // causes the reset.
        for (int i = 0; i < result.Length; ++i)
        {
            sbyte t = initialTypes[i];
            if (t == B || t == S)
            {
                // Rule L1, clauses one and two.
                result[i] = paragraphEmbeddingLevel;

                // Rule L1, clause three.
                for (int j = i - 1; j >= 0; --j)
                {
                    if (isWhitespace(initialTypes[j]))
                    { // including format
                      // codes
                        result[j] = paragraphEmbeddingLevel;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        // Rule L1, clause four.
        int start = 0;
        for (int i = 0; i < linebreaks.Length; ++i)
        {
            int limit = linebreaks[i];
            for (int j = limit - 1; j >= start; --j)
            {
                if (isWhitespace(initialTypes[j]))
                { // including format codes
                    result[j] = paragraphEmbeddingLevel;
                }
                else
                {
                    break;
                }
            }

            start = limit;
        }

        return result;
    }

    /**
     * Return reordering array breaking lines at offsets in linebreaks.
     * <p>
     * The reordering array maps from a visual index to a logical index. Lines
     * are concatenated from left to right. So for example, the fifth character
     * from the left on the third line is
     *
     * <pre>
     * getReordering(linebreaks)[linebreaks[1] + 4]
     * </pre>
     *
     * (linebreaks[1] is the position after the last character of the second
     * line, which is also the index of the first character on the third line,
     * and adding four gets the fifth character from the left).
     * <p>
     * The linebreaks array must include at least one value. The values must be
     * in strictly increasing order (no duplicates) between 1 and the length of
     * the text, inclusive. The last value must be the length of the text.
     *
     * @param linebreaks
     *            the offsets at which to break the paragraph.
     */
    public int[] getReordering(int[] linebreaks)
    {
        validateLineBreaks(linebreaks, textLength);

        sbyte[] levels = getLevels(linebreaks);

        return computeMultilineReordering(levels, linebreaks);
    }

    /**
     * Return multiline reordering array for a given level array. Reordering
     * does not occur across a line break.
     */
    private static int[] computeMultilineReordering(sbyte[] levels, int[] linebreaks)
    {
        int[] result = new int[levels.Length];

        int start = 0;
        for (int i = 0; i < linebreaks.Length; ++i)
        {
            int limit = linebreaks[i];

            sbyte[] templevels = new sbyte[limit - start];
            Array.Copy(levels, start, templevels, 0, templevels.Length);

            int[] temporder = computeReordering(templevels);
            for (int j = 0; j < temporder.Length; ++j)
            {
                result[start + j] = temporder[j] + start;
            }

            start = limit;
        }

        return result;
    }

    /**
     * Return reordering array for a given level array. This reorders a single
     * line. The reordering is a visual to logical map. For example, the
     * leftmost char is string.charAt(order[0]). Rule L2.
     */
    private static int[] computeReordering(sbyte[] levels)
    {
        int lineLength = levels.Length;

        int[] result = new int[lineLength];

        // initialize order
        for (int i = 0; i < lineLength; ++i)
        {
            result[i] = i;
        }

        // locate highest level found on line.
        // Note the rules say text, but no reordering across line bounds is
        // performed, so this is sufficient.
        sbyte highestLevel = 0;
        sbyte lowestOddLevel = MAX_DEPTH + 2;
        for (int i = 0; i < lineLength; ++i)
        {
            sbyte level = levels[i];
            if (level > highestLevel)
            {
                highestLevel = level;
            }
            if (((level & 1) != 0) && level < lowestOddLevel)
            {
                lowestOddLevel = level;
            }
        }

        for (int level = highestLevel; level >= lowestOddLevel; --level)
        {
            for (int i = 0; i < lineLength; ++i)
            {
                if (levels[i] >= level)
                {
                    // find range of text at or above this level
                    int start = i;
                    int limit = i + 1;
                    while (limit < lineLength && levels[limit] >= level)
                    {
                        ++limit;
                    }

                    // reverse run
                    for (int j = start, k = limit - 1; j < k; ++j, --k)
                    {
                        int temp = result[j];
                        result[j] = result[k];
                        result[k] = temp;
                    }

                    // skip to end of level run
                    i = limit;
                }
            }
        }

        return result;
    }

    /**
     * Return the base level of the paragraph.
     */
    public sbyte getBaseLevel()
    {
        return paragraphEmbeddingLevel;
    }

    // --- internal utilities -------------------------------------------------

    /**
     * Return true if the type is considered a whitespace type for the line
     * break rules.
     */
    private static bool isWhitespace(sbyte biditype)
    {
        switch (biditype)
        {
            case LRE:
            case RLE:
            case LRO:
            case RLO:
            case PDF:
            case LRI:
            case RLI:
            case FSI:
            case PDI:
            case BN:
            case WS:
                return true;
            default:
                return false;
        }
    }

    /**
     * Return true if the type is one of the types removed in X9.
     * Made public so callers can duplicate the effect.
     */
    public static bool isRemovedByX9(sbyte biditype)
    {
        switch (biditype)
        {
            case LRE:
            case RLE:
            case LRO:
            case RLO:
            case PDF:
            case BN:
                return true;
            default:
                return false;
        }
    }

    /**
     * Return the strong type (L or R) corresponding to the level.
     */
    private static sbyte typeForLevel(int level)
    {
        return ((level & 0x1) == 0) ? L : R;
    }

    /**
     * Set levels from start up to (but not including) limit to newLevel.
     */
    private static void setLevels(sbyte[] levels, int start, int limit, sbyte newLevel)
    {
        for (int i = start; i < limit; ++i)
        {
            levels[i] = newLevel;
        }
    }

    // --- input validation ---------------------------------------------------

    /**
     * Throw exception if type array is invalid.
     */
    private static void validateTypes(sbyte[] types)
    {
        if (types == null)
        {
            throw new ArgumentException("types is null");
        }
        for (int i = 0; i < types.Length; ++i)
        {
            if (types[i] < TYPE_MIN || types[i] > TYPE_MAX)
            {
                throw new ArgumentException("illegal type value at " + i + ": " + types[i]);
            }
        }
        for (int i = 0; i < types.Length - 1; ++i)
        {
            if (types[i] == B)
            {
                //throw new ArgumentException("B type before end of paragraph at index: " + i);
            }
        }
    }

    /**
     * Throw exception if paragraph embedding level is invalid. Special
     * allowance for implicitEmbeddinglevel so that default processing of the
     * paragraph embedding level as implicit can still be performed when
     * using this API.
     */
    private static void validateParagraphEmbeddingLevel(sbyte paragraphEmbeddingLevel)
    {
        if (paragraphEmbeddingLevel != implicitEmbeddingLevel &&
                paragraphEmbeddingLevel != 0 &&
                paragraphEmbeddingLevel != 1)
        {
            throw new ArgumentException("illegal paragraph embedding level: " + paragraphEmbeddingLevel);
        }
    }

    /**
     * Throw exception if line breaks array is invalid.
     */
    private static void validateLineBreaks(int[] linebreaks, int textLength)
    {
        int prev = 0;
        for (int i = 0; i < linebreaks.Length; ++i)
        {
            int next = linebreaks[i];
            if (next <= prev)
            {
                throw new ArgumentException("bad linebreak: " + next + " at index: " + i);
            }
            prev = next;
        }
        if (prev != textLength)
        {
            throw new ArgumentException("last linebreak must be at " + textLength);
        }
    }

    /**
     * Throw exception if pairTypes array is invalid
     */
    private static void validatePbTypes(sbyte[] pairTypes)
    {
        if (pairTypes == null)
        {
            throw new ArgumentException("pairTypes is null");
        }
        for (int i = 0; i < pairTypes.Length; ++i)
        {
            if (pairTypes[i] < BidiPBAReference.n || pairTypes[i] > BidiPBAReference.c)
            {
                throw new ArgumentException("illegal pairType value at " + i + ": " + pairTypes[i]);
            }
        }
    }

    /**
     * Throw exception if pairValues array is invalid or doesn't match pairTypes in length
     * Unfortunately there's little we can do in terms of validating the values themselves
     */
    private static void validatePbValues(int[] pairValues, sbyte[] pairTypes)
    {
        if (pairValues == null)
        {
            throw new ArgumentException("pairValues is null");
        }
        if (pairTypes.Length != pairValues.Length)
        {
            throw new ArgumentException("pairTypes is different length from pairValues");
        }
    }

    /**
     * static entry point for testing using several arrays of direction and other types and an externally supplied
     * paragraph embedding level. The embedding level may be 0, 1 or 2.
     * <p>
     * 2 means to apply the default algorithm (rules P2 and P3), 0 is for LTR
     * paragraphs, and 1 is for RTL paragraphs.
     *
     * @param types
     *            the directional types array
     * @param pairTypes
     *           the paired bracket types array
     * @param pairValues
     * 			 the paired bracket values array
     * @param paragraphEmbeddingLevel
     *            the externally supplied paragraph embedding level.
     */
    public static BidiReference analyzeInput(sbyte[] types, sbyte[] pairTypes, int[] pairValues, sbyte paragraphEmbeddingLevel)
    {
        BidiReference bidi = new BidiReference(types, pairTypes, pairValues, paragraphEmbeddingLevel);
        return bidi;
    }

}
