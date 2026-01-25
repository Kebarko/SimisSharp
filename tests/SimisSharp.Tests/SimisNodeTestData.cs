using System.Collections;

namespace KE.MSTS.SimisSharp.Tests;

public class SimisNodeTestData : IEnumerable<object[]>
{
    // A
    // ├── B
    // │   ├── E
    // │   └── F
    // ├── C
    // │   ├── G
    // │   └── H
    // ├── B
    // │   ├── E
    // │   ├── I
    // │   └── J
    // └── D
    //     ├── K
    //     └── L

    // Level 1
    private readonly SimisNode nodeA;

    // Level 2
    private readonly SimisNode nodeAB;
    private readonly SimisNode nodeAC;
    private readonly SimisNode nodeAB2;
    private readonly SimisNode nodeAD;

    // Level 3 – children of B
    private readonly SimisNode nodeABE;
    private readonly SimisNode nodeABF;

    // Level 3 – children of C
    private readonly SimisNode nodeACG;
    private readonly SimisNode nodeACH;

    // Level 3 – children of second B
    private readonly SimisNode nodeABE2;
    private readonly SimisNode nodeABI;
    private readonly SimisNode nodeABJ;

    // Level 3 – children of D
    private readonly SimisNode nodeADK;
    private readonly SimisNode nodeADL;

    public SimisNodeTestData()
    {
        // Level 1 (root)
        nodeA = new SimisNode("A");

        // Level 2
        nodeAB = new SimisNode("B");
        nodeAC = new SimisNode("C");
        nodeAB2 = new SimisNode("B");
        nodeAD = new SimisNode("D");

        nodeA.AddNode(nodeAB);
        nodeA.AddNode(nodeAC);
        nodeA.AddNode(nodeAB2);
        nodeA.AddNode(nodeAD);

        // Level 3 - children of B
        nodeABE = new SimisNode("E");
        nodeABF = new SimisNode("F");
        nodeAB.AddNode(nodeABE);
        nodeAB.AddNode(nodeABF);

        // Level 3 - children of C
        nodeACG = new SimisNode("G");
        nodeACH = new SimisNode("H");
        nodeAC.AddNode(nodeACG);
        nodeAC.AddNode(nodeACH);

        // Level 3 - children of second B
        nodeABE2 = new SimisNode("E");
        nodeABI = new SimisNode("I");
        nodeABJ = new SimisNode("J");
        nodeAB2.AddNode(nodeABE2);
        nodeAB2.AddNode(nodeABI);
        nodeAB2.AddNode(nodeABJ);

        // Level 3 - children of D
        nodeADK = new SimisNode("K");
        nodeADL = new SimisNode("L");
        nodeAD.AddNode(nodeADK);
        nodeAD.AddNode(nodeADL);
    }

    public IEnumerator<object[]> GetEnumerator()
    {
        yield return new object[]
        {
            nodeA,
            "B/E",
            new[] { nodeABE, nodeABE2 }
        };

        yield return new object[]
        {
            nodeA,
            "B[2]/J",
            new[] { nodeABJ }
        };

        yield return new object[]
        {
            nodeADL,
            "..\\K",
            new[] { nodeADK }
        };

        yield return new object[]
        {
            nodeAB,
            "/C/[2]",
            new[] { nodeACH }
        };
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
