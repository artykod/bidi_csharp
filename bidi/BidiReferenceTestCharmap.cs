/*
 * (C) Copyright IBM Corp. 1999, All Rights Reserved
 * (C) Copyright Google Inc. 2013, All Rights Reserved
 * (C) Copyright ASMUS, Inc. 2013, All Rights Reserved
 *
 * Distributed under the Terms of Use in http://www.unicode.org/copyright.html.
 */


using System.Text;
/**
* A class that maps ASCII characters to bidi direction types, used for testing purposes.
* This class should not be used as a model for access to or storage of this information.
*
* @author Doug Felt
*/
public abstract class BidiReferenceTestCharmap
{

    /** Charmap instance that maps portions of ASCII to strong format codes. */
    public static readonly BidiReferenceTestCharmap TEST_ENGLISH = new TestEnglish();

    /** Charmap instance that maps portions of ASCII to AL, AN. */
    public static readonly BidiReferenceTestCharmap TEST_MIXED = new TestMixed();

    /** Charmap instance that maps portions of ASCII to R. */
    public static readonly BidiReferenceTestCharmap TEST_HEBREW = new TestHebrew();

    /** Charmap instance that maps portions of ASCII to AL, AN, R. */
    public static readonly BidiReferenceTestCharmap TEST_ARABIC = new TestArabic();

    /** Charmap instance that maps portions of ASCII to R, and brackets to ON for PBA. */
    public static readonly BidiReferenceTestCharmap TEST_PBA = new TestPBA();

    public static readonly BidiReferenceTestCharmap TEST_MIXED_PBA = new TestMixedPBA();


    private const sbyte L = BidiReference.L;
    private const sbyte LRE = BidiReference.LRE;
    private const sbyte LRO = BidiReference.LRO;
    private const sbyte R = BidiReference.R;
    private const sbyte AL = BidiReference.AL;
    private const sbyte RLE = BidiReference.RLE;
    private const sbyte RLO = BidiReference.RLO;
    private const sbyte PDF = BidiReference.PDF;
    private const sbyte EN = BidiReference.EN;
    private const sbyte ES = BidiReference.ES;
    private const sbyte ET = BidiReference.ET;
    private const sbyte AN = BidiReference.AN;
    private const sbyte CS = BidiReference.CS;
    private const sbyte NSM = BidiReference.NSM;
    private const sbyte BN = BidiReference.BN;
    private const sbyte B = BidiReference.B;
    private const sbyte S = BidiReference.S;
    private const sbyte WS = BidiReference.WS;
    private const sbyte ON = BidiReference.ON;
    private const sbyte RLI = BidiReference.RLI;
    private const sbyte LRI = BidiReference.LRI;
    private const sbyte FSI = BidiReference.FSI;
    private const sbyte PDI = BidiReference.PDI;

    private const sbyte TYPE_MIN = BidiReference.TYPE_MIN;
    private const sbyte TYPE_MAX = BidiReference.TYPE_MAX;

    private static readonly string[] typenames = BidiReference.typenames;

    /**
     * Return the name of this mapping.
     */
    public abstract string getName();

    /**
     * Return the bidi direction codes corresponding to the ASCII characters in the string.
     * @param str the string
     * @return an array of bidi direction codes
     */
    public sbyte[] getCodes(string str)
    {
        return getCodes(str.ToCharArray());
    }

    /**
     * Return the bidi direction codes corresponding to the ASCII characters in the array.
     * @param chars the array of ASCII characters
     * @return an array of bidi direction codes
     */
    public sbyte[] getCodes(char[] chars)
    {
        return getCodes(chars, 0, chars.Length);
    }

    /**
     * Return the bidi direction codes corresponding to the ASCII characters in the subrange
     * of the array.
     * @param chars the array of ASCII characters
     * @param charstart the start of the subrange to use
     * @param count the number of characters in the subrange to use
     * @return an array of bidi direction codes
     */
    public sbyte[] getCodes(char[] chars, int charstart, int count)
    {
        sbyte[] result = new sbyte[count];
        convert(chars, charstart, result, 0, count);
        return result;
    }

    /**
     * Display the mapping from ASCII to bidi direction codes using the provided PrintWriter.
     */
    public abstract void dumpInfo(PrintWriter w);

    /**
     * Convert a subrange of characters to direction codes and place into the code array.
     *
     * @param chars the characters to convert
     * @param charStart the start position in the chars array
     * @param codes the destination array for the direction codes
     * @param codeStart the start position in the codes array
     * @param count the number of characters to convert to direction codes
     */
    public abstract void convert(char[] chars, int charStart, sbyte[] codes, int codeStart, int count);

    /**
     *  Diagnostic utility to list array of bidi direction codes
     *
     * @param w - where to output
     * @param codes - array of bidi direction codes
     */
    public abstract void dumpCodes(PrintWriter w, sbyte[] codes);

    //
    // Default implementation classes
    //

    /**
     * Default implementation that maps ASCII to all bidi types.
     *
     * This is the base class for TestArabic, TestHebrew, and TestMixed mappings.
     */
    public class DefaultCharmap : BidiReferenceTestCharmap
    {
        protected string name;
        protected sbyte[] map;

        /**
         * Initialize to default mapping, and define name.
         */
        public DefaultCharmap(string name)
        {

            this.name = name;

            map = (sbyte[])baseMap.Clone();

            // steal some printable characters for format controls, etc
            // finalize basic mapping
            setMap(RLO, "}");
            setMap(LRO, "{");
            setMap(PDF, "^");
            setMap(RLE, "]");
            setMap(LRE, "[");
            setMap(RLI, ">");
            setMap(LRI, "<");
            setMap(FSI, "?");
            setMap(PDI, "=");
            setMap(NSM, "~");
            setMap(BN, "`");
            setMap(B, "|"); // visible character for convenience
            setMap(S, "_"); // visible character for convenience
        }

        /**
         * Utility used to change the mapping.
         */
        protected void setMap(sbyte value, string chars)
        {
            for (int i = 0; i < chars.Length; ++i)
            {
                map[chars[i]] = value;
            }
        }

        /**
         * Standard character mapping for Latin-1. Protected so that it can be
         * directly accessed by subclasses.
         */
        protected static readonly sbyte[] baseMap = {
            ON,  ON,  ON,  ON,  ON,  ON,  ON,  ON,  // 00-07 c0 c0 c0 c0 c0 c0 c0 c0
            ON,   S,   B,   S,   B,   B,  ON,  ON,  // 08-0f c0 HT LF VT FF CR c0 c0
            ON,  ON,  ON,  ON,  ON,  ON,  ON,  ON,  // 10-17 c0 c0 c0 c0 c0 c0 c0 c0
            ON,  ON,  ON,  ON,   B,   B,   B,   S,  // 18-1f c0 c0 c0 c0 FS GS RS US
            WS,  ON,  ON,  ET,  ET,  ET,  ON,  ON,  // 20-27     !  "  #  $  %  &  '
            ON,  ON,  ON,  ET,  CS,  ET,  CS,  ES,  // 28-2f  (  )  *  +  ,  -  .  /
            EN,  EN,  EN,  EN,  EN,  EN,  EN,  EN,  // 30-37  0  1  2  3  4  5  6  7
            EN,  EN,  CS,  ON,  ON,  ON,  ON,  ON,  // 38-3f  8  9  :  ;  <  =  >  ?
            ON,   L,   L,   L,   L,   L,   L,   L,  // 40-47  @  A  B  C  D  E  F  G
             L,   L,   L,   L,   L,   L,   L,   L,  // 48-4f  H  I  J  K  L  M  N  O
             L,   L,   L,   L,   L,   L,   L,   L,  // 50-57  P  Q  R  S  T  U  V  W
             L,   L,   L,  ON,  ON,  ON,  ON,   S,  // 58-5f  X  Y  Z  [  \  ]  ^  _
            ON,   L,   L,   L,   L,   L,   L,   L,  // 60-67  `  a  b  c  d  e  f  g
             L,   L,   L,   L,   L,   L,   L,   L,  // 68-6f  h  i  j  k  l  m  n  o
             L,   L,   L,   L,   L,   L,   L,   L,  // 70-77  p  q  r  s  t  u  v  w
             L,   L,   L,  ON,  ON,  ON,  ON,  ON   // 78-7f  x  y  z  {  |  }  ~  DEL
        };

        /**
         * Return the name.
         */
        public override string getName()
        {
            return name;
        }

        /**
         * Standard implementation of dumpInfo that displays, for each bidi
         * direction type, the characters that are mapped to that type.
         */
        public override void dumpInfo(PrintWriter w)
        {
            // dump mapping table
            // organized by type and coalescing printable characters

            w.print(name);
            for (sbyte t = TYPE_MIN; t <= TYPE_MAX; ++t)
            {
                w.println();
                w.print("   ".Substring(typenames[t].Length) + typenames[t] + ": ");
                int runStart = 0;
                bool first = true;
                while (runStart < map.Length)
                {
                    while (runStart < map.Length && map[runStart] != t)
                    {
                        ++runStart;
                    }
                    if (runStart < map.Length)
                    {
                        int runEnd = runStart + 1;
                        while (runEnd < map.Length && map[runEnd] == t)
                        {
                            ++runEnd;
                        }
                        if (first)
                        {
                            first = false;
                        }
                        else
                        {
                            w.print(",");
                        }
                        switch (runEnd - runStart)
                        {
                            case 1:
                                dumpChar(runStart, w);
                                break;
                            case 2:
                                dumpChar(runStart, w);
                                w.print(",");
                                dumpChar(runEnd - 1, w);
                                break;
                            default:
                                // only use ranges for a-z, A-Z, 0-9, c0 (hex display)
                                if ((runStart >= 'a' && (runEnd - 1 <= 'z')) ||
                                        (runStart >= 'A' && (runEnd - 1 <= 'Z')) ||
                                        (runStart >= '0' && (runEnd - 1 <= '9')) ||
                                        (runStart >= 0x0 && (runEnd - 1 <= 0x1f)))
                                {

                                    dumpChar(runStart, w);
                                    w.print("-");
                                    dumpChar(runEnd - 1, w);
                                }
                                else
                                {
                                    dumpChar(runStart, w);
                                    runEnd = runStart + 1;
                                }
                                break;
                        }

                        runStart = runEnd;
                    }
                }
            }
            w.println();
            w.println();
        }

        /**
         * Utility used to output a 'name' of single character, passed as an
         * int. Printable characters are represented as themselves,
         * non-printable characters as hex values. Comma, hyphen, and space are
         * represented as strings surrounded by square brackets.
         *
         * @param i
         *            the int value of the character
         * @param w
         *            the PrintWriter on which to output the representation of
         *            the character
         */
        protected static void dumpChar(int i, PrintWriter w)
        {
            /*final char c = (char)i;

            if (c == ',') {
                w.print("[comma]");
            } else if (c == '-') {
                w.print("[hyphen]");
            } else if (c == ' ') {
                w.print("[space]");
            } else if (i > 0x20 && i < 0x7f) {
                w.print(c);
            } else {
                w.print("0x" + int.toHexString(i));
            }*/
        }

        /**
         * Standard implementation of convert.
         */
        public override void convert(char[] chars, int charStart, sbyte[] codes, int codeStart, int count)
        {
            for (int i = 0; i < count; ++i)
            {
                codes[codeStart + i] = map[chars[charStart + i]];
            }
        }

        /**
         *  Standard implementation of dumpCodes
         */
        public override void dumpCodes(PrintWriter w, sbyte[] codes)
        {
            StringBuilder s = new StringBuilder();
            s.Append("[");
            foreach (sbyte b in codes)
            {
                s.Append(typenames[b] + ", ");
            }
            // remove trailing commas
            if (codes.Length > 2)
            {
                s.Remove(s.Length - 2, 2);
            }
            s.Append("]");
            w.println(s.ToString());
        }

    }

    // 'English' mapping just implements the default, naming it "English."
    // Not too interesting, as there are no AL, R, or AN characters.  It does provide
    // mappings to the explicit format codes.

    public class TestEnglish : DefaultCharmap
    {
        public TestEnglish() : base("")
        {
        }
    }

    // Mixed arabic and hebrew test character mapping.
    //
    // In practice, this is not so convenient for experimenting with the algorithm, as
    // it is easy to forget the boundaries between the hebrew and arabic ranges of the
    // upper case letters and the english and arabic ranges of the numbers.

    public class TestMixed : DefaultCharmap
    {
        public TestMixed() : base("")
        {
            setMap(AL, "ABCDEFGHIJKLM");
            setMap(R, "NOPQRSTUVWXYZ");
            setMap(AN, "56789");
        }
    }

    // Hebrew test character mapping.

    public class TestHebrew : DefaultCharmap
    {
        public TestHebrew() : base("")
        {
            setMap(R, "ABCDEFGHIJKLMNOPQRSTUVWXYZ");
        }
    }

    //  Bidi Paired Brackets Algorithm test mapping
    //
    //  This maps the ASCII brackets to ON, allowing them to be processed
    //	as actual bracket characters. Also sets some digits to AN
    //  includes a re-mapping of some other characters to the isolate 
    //  controls so those can be tested as well.
    //

    public class TestPBA : DefaultCharmap
    {
        public TestPBA() : base("")
        {
            setMap(R, "ABCDEFGHIJKLMNOPQRSTUVWXYZ");
            setMap(ON, "[]{}()<>");
            setMap(AN, "56789");
            setMap(RLI, "*"); // can't use brackets
            setMap(LRI, "!"); // can't use brackets
                              // currently no way to do RLE,RLO,PDF, etc.
        }
    }


    // Arabic mapping with Arabic numbers

    public class TestArabic : DefaultCharmap
    {
        public TestArabic() : base("")
        {
            setMap(AL, "ABCDEFGHIJKLMNOPQRSTUVWXYZ");
            setMap(AN, "0123456789");
        }
    }

    public class TestMixedPBA : DefaultCharmap
    {
        public TestMixedPBA() : base("")
        {
            setMap(L, "\n\r");
            setMap(R, "ABCDEFGHIJKLMNOPQRSTUVWXYZ");
        }
    }
}
