using KE.MSTS.SimisSharp.IO;
using System.Collections;

namespace KE.MSTS.SimisSharp.Tests;

internal class SimisDocumentTestData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {

        yield return new object[]
        {
            string.Empty,
            string.Empty,
        };

        yield return new object[]
        {
"""
 SIMISA@@@@@@@@@@JINX0r1t______
 
TPoleConfigData ( 1
  TPoleConfig ( 4
    Filename ( telepole.s )
    Shadow ( teleshad.s )
    Separation(10)Separation(10)
    Wire ( 0.0 6.7 0.391 )
    Wire ( 0.0 "6.7 -0.39" )
    Wire ( 0.0 6.2 0.228 )
    comment ( Wire ( 0.0   6.2 -0.219 ) )
    Wire ( 0.0 6.7 0.391 )
  )
) 
""",
"""
SIMISA@@@@@@@@@@JINX0r1t______

TPoleConfigData ( 1
  TPoleConfig ( 4
    Filename ( telepole.s )
    Shadow ( teleshad.s )
    Separation ( 10 )
    Separation ( 10 )
    Wire ( 0.0 6.7 0.391 )
    Wire ( 0.0 "6.7 -0.39" )
    Wire ( 0.0 6.2 0.228 )
    comment ( Wire ( 0.0   6.2 -0.219 ) )
    Wire ( 0.0 6.7 0.391 )
  )
)

""",
            new WriterOptions { IndentCharacter = ' ', IndentSize = 2 },
        };

        yield return new object[]
        {
"""
 SIMISA@@@@@@@@@@JINX0r1t______ 
""",
"""
SIMISA@@@@@@@@@@JINX0r1t______

""",
        };

        yield return new object[]
        {
"""
 SIMISA@@@@@@@@@@JINX0r1t______
 
8
""",
"""
SIMISA@@@@@@@@@@JINX0r1t______

8

""",
        };

        yield return new object[]
        {
"""
SIMISA@@@@@@@@@@JINX0F0t______

8
Forest ( "JP1Bush1" "JP2AutoBush1.ace" 3.50f 2.5f 0.9f 1.1f )
Forest ( "JP1Bush2" "JP2AutoBush2.ace" 4.5f 3.2f 0.9f 1.1f )
Forest ( "JP1Bush3" "JP2AutoBush3.ace" 4.6f 3.4f 0.9f 1.1f)
Forest ( "JP1Tree1" "JP2AutoTree1.ace" 16.0f 20.0f 0.9f 1.1f )
Forest ( "JP1Tree2" "JP2AutoTree2.ace" 14.0f 18.0f 0.9f 1.1f )
Forest ( "JP1Tree3" "JP2AutoTree3.ace" 10.0f 14.0f 0.9f 1.1f)
Forest ( "UKTree1" "tree8.ace" 20.0f 18.0f 0.9f 1.1f)
Forest ( "UKTree2" "tree4.ace" 19.0f 17.0f 0.9f 1.1f)
""",
"""
SIMISA@@@@@@@@@@JINX0F0t______

8
Forest ( JP1Bush1 JP2AutoBush1.ace 3.50f 2.5f 0.9f 1.1f )
Forest ( JP1Bush2 JP2AutoBush2.ace 4.5f 3.2f 0.9f 1.1f )
Forest ( JP1Bush3 JP2AutoBush3.ace 4.6f 3.4f 0.9f 1.1f )
Forest ( JP1Tree1 JP2AutoTree1.ace 16.0f 20.0f 0.9f 1.1f )
Forest ( JP1Tree2 JP2AutoTree2.ace 14.0f 18.0f 0.9f 1.1f )
Forest ( JP1Tree3 JP2AutoTree3.ace 10.0f 14.0f 0.9f 1.1f )
Forest ( UKTree1 tree8.ace 20.0f 18.0f 0.9f 1.1f )
Forest ( UKTree2 tree4.ace 19.0f 17.0f 0.9f 1.1f )

""",
        };

        yield return new object[]
        {
"""
SIMISA@@@@@@@@@@JINX0T0t______

TrackSections ( 2
  TrackSection (
    SectionCurve ( 0 ) 376 8 0
  )
  TrackSection (
    SectionCurve ( 1 ) 378 -0.178 172.42
  )
)
SectionIdx ( 2
  TrackPath ( 263 1 376 )
  TrackPath ( 264 1 378 )
)
""",
"""
SIMISA@@@@@@@@@@JINX0T0t______

TrackSections ( 2
	TrackSection (
		SectionCurve ( 0 ) 376 8 0
	)
	TrackSection (
		SectionCurve ( 1 ) 378 -0.178 172.42
	)
)
SectionIdx ( 2
	TrackPath ( 263 1 376 )
	TrackPath ( 264 1 378 )
)

""",
            new WriterOptions { IndentCharacter = '\t', IndentSize = 1 },
        };

        yield return new object[]
        {
"""
SIMISA@@@@@@@@@@JINX0t1t______
shape(US2BNSFCar.s ESD_Detail_Level(0)ESD_Software_DLev(3)ESD_Alternative_Texture(0)ESD_Bounding_Box(-1.60179 0.8024 -10.51718 1.5172 3.77019 10.48826))
""",
"""
SIMISA@@@@@@@@@@JINX0t1t______
shape ( US2BNSFCar.s
	ESD_Detail_Level ( 0 )
	ESD_Software_DLev ( 3 )
	ESD_Alternative_Texture ( 0 )
	ESD_Bounding_Box ( -1.60179 0.8024 -10.51718 1.5172 3.77019 10.48826 )
)

""",
        };

        yield return new object[]
        {
"""
SIMISA@@@@@@@@@@JINX0x1t______



Tr_SMS (

Skip( mono loop & spot )

	ScalabiltyGroup( 3
		Activation (
			ExternalCam ()
			Distance    (500)
		)
		Deactivation (
			PassengerCam	()
			CabCam			()
			Distance    	(500)
		)
		Volume ( 1.0)
		Streams ( 1
			Stream (
				Priority ( 2 )
				Volume ( 1.0 )
				Triggers ( 1
					Initial_Trigger ( 
						StartLoop ( 1
							File ( "gen_1.wav" -1 )
							SelectionMethod ( RandomSelection )
						)
					)
				)
			)
		)
	)
)
""",
"""
SIMISA@@@@@@@@@@JINX0x1t______

Tr_SMS (

	Skip ( mono loop & spot )

	ScalabiltyGroup ( 3
		Activation (
			ExternalCam ( )
			Distance ( 500 )
		)
		Deactivation (
			PassengerCam ( )
			CabCam ( )
			Distance ( 500 )
		)
		Volume ( 1.0 )
		Streams ( 1
			Stream (
				Priority ( 2 )
				Volume ( 1.0 )
				Triggers ( 1
					Initial_Trigger (
						StartLoop ( 1
							File ( gen_1.wav -1 )
							SelectionMethod ( RandomSelection )
						)
					)
				)
			)
		)
	)
)

""",
            new WriterOptions { IndentCharacter = '\t', IndentSize = 1 },
        };

        yield return new object[]
        {
"""
SIMISA@@@@@@@@@@JINX0w1t______

Skip
(
  Screens in this file are ordered as follows:

  Main Menu                   #0
  Activity & Route Selection  #1
  Introductory Train Ride     #2
  Toolkit                     #3
  RouteEditor                 #4
)



TrGUI_Maingui
(
  TrGUI_Screen
  (
    TrGUI_Region
    (
      TrGUI_Region_Name    ( mainmenu )
      TrGUI_Region_Surf    ( startsur )
      TrGUI_Region_Area    ( 0 0 800 600 )
      TrGUI_Region_Cursor  ( arrow_cur )
	  TrGUI_Help_Context	( 1 )
    )
    TrGUI_Widget ( start )
    TrGUI_Icon_Set ( 1 )
    TrGUI_Loading_Screen ( trainloading )
  )
)
""",
"""
SIMISA@@@@@@@@@@JINX0w1t______

TrGUI_Maingui (
  TrGUI_Screen (
    TrGUI_Region (
      TrGUI_Region_Name ( mainmenu )
      TrGUI_Region_Surf ( startsur )
      TrGUI_Region_Area ( 0 0 800 600 )
      TrGUI_Region_Cursor ( arrow_cur )
      TrGUI_Help_Context ( 1 )
    )
    TrGUI_Widget ( start )
    TrGUI_Icon_Set ( 1 )
    TrGUI_Loading_Screen ( trainloading )
  )
)

""",
            new WriterOptions { IndentCharacter = ' ', IndentSize = 2, PreserveComments = false },
        };

        yield return new object[]
        {
"""
SIMISA@@@@@@@@@@JINX0t1t______

shape ( Gp38.s

	ESD_Detail_Level ( 0 )


	ESD_Software_DLev ( 2 )



	ESD_Alternative_Texture ( 0 )




	ESD_Bounding_Box ( -1.52538 0.23572 -9.12204 1.57886 4.76894 9.25626 )





)
""",
"""
SIMISA@@@@@@@@@@JINX0t1t______

shape ( Gp38.s

	ESD_Detail_Level ( 0 )

	ESD_Software_DLev ( 2 )

	ESD_Alternative_Texture ( 0 )

	ESD_Bounding_Box ( -1.52538 0.23572 -9.12204 1.57886 4.76894 9.25626 )

)

""",
        };

        yield return new object[]
        {
"""
SIMISA@@@@@@@@@@JINX0t1t______

Adheasion ( 0.2 0.4 2 0 )
DerailRailHeight ( 4cm )
DerailRailForce ( 2.5*250e3lbs )
DerailBufferForce ( 200kN )
NumWheels (	8 )
Friction (
	100N/m/s	1	-1mph	0	1
	5.1N/rad/s	1	-1rad/s	0	1
)
AntiSlip ("")
""",
"""
SIMISA@@@@@@@@@@JINX0t1t______

Adheasion ( 0.2 0.4 2 0 )
DerailRailHeight ( 4cm )
DerailRailForce ( 2.5*250e3lbs )
DerailBufferForce ( 200kN )
NumWheels ( 8 )
Friction (
	100N/m/s 1 -1mph 0 1
	5.1N/rad/s 1 -1rad/s 0 1
)
AntiSlip ( )

""",
            new WriterOptions{ ValuesInColumns = new Dictionary<string, int> { { "Friction", 5 } }}
        };
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
