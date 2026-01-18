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
            new WriterOptions { IndentSize = 2, IndentCharacter = ' '  },
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
            new WriterOptions { PreserveQuotes = false },
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
            new WriterOptions { IndentSize = 2, IndentCharacter = ' ', PreserveComments = false },
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
AntiSlip ()
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

        yield return new object[]
        {
"""
SIMISA@@@@@@@@@@JINX0t1t______

EngineOperatingProcedures (
"To move forward\n"+
"1.	Press Shift+E if necessary to move the Speed Gear handle to neutral.\n"+
"2. Hold down the A key as necessary to move the Throttle to 0%.\n"+
"3.	Press the W key to move the Reverser to Forward.\n"+
"4. Press E to move the Speed Gear handle to 1.\n"+
"5.	Hold down the ; (SEMICOLON) key to release the train brakes.\n"+
"6.	Press the D key to move increase the Throttle setting.\n\n"+  
"To coast\n"+
"1.	Hold down the A key to reduce the Throttle to 0%.\n"+
"2.	Press Shift+E to move the Speed Gear handle from 1 to Neutral.\n\n"+
"To resume powered operation\n"+
"1.	If traveling under 50 km/h, press E once to move the Speed Gear handle to 1. If traveling over 50 km/h, press E twice to move the Speed Gear handle to 2.\n"+
"2.	Press the D key to advance the Throttle.\n\n"+
"To operate the KIHA 31 at high speed (over 50 km/h)\n"+
"1.	Press the E key to move the Speed Gear handle to 2.\n"+
"2.	Press the D key to advance the Throttle.\n\n"+
"To return the KIHA 31 to low speed operation (under 50 Km/h)\n"+
"1.	Press Shift+E to move the Speed Gear handle to 1.\n\n"+
"To slow the train\n"+
"1.	Press the A key to move the Throttle to a lower setting.\n"+
"2.	Use the brakes to further reduce speed if desired.\n\n"+
"To apply the train brakes\n"+
"1.	Hold down the ' (APOSTROPHE) key.\n\n"+
"To release the train brakes\n"+
"1. Hold down the ; (SEMICOLON) key.\n\n"+
"To operate the Engine Brake\n"+
"1.	Press Shift+E to move the Speed Gear handle to 2.\n"+
"2.	Hold down the A key to move the Throttle to 0%.\n"+
"3.	Press the P key to activate the Engine Brake.\n\n"+
"To deactivate the Engine Brake\n"+
"1.	Press the D key to advance the Throttle.\n\n"+
"To back up\n"+
"1.	Bring your train to a full stop.\n"+
"2. Hold down the A key to move the Throttle to 0%.\n"+
"3. Press Shift+E to move the Speed Gear handle to Neutral.\n"+
"4.	Press the S key twice to move the Reverser to the Reverse position.\n"+
"5.	Press the ; [SEMICOLON] key to release the brakes.\n"+
"6. Press E to move the Speed Gear handle to 1.\n"+
"7.	Press the D key to slowly advance the Throttle.\n\n"+
"To load and unload passengers\n"+
"1. Stop at a station.\n"+
"2. Press the Enter key.\n"+
"3. Wait for the \"permission to depart\" buzzer.\n"+
"TIP: Use the Next Station driving aid (F10) to monitor loading and unloading.\n\n"+
"To couple to a car or locomotive\n"+
"1. Coast into the unit at a very slow speed, about 4 km/h.\n"+
"Tip: To use the Coupling view, press the 6 key.\n"+
"2. Press F9 to open the Train Operations window.\n"+
"3. If the icon for the unit you coupled to is red, the handbrake is on. Double-click the car to release the handbrake.\n"+
"4. Close the Train Operations window.\n\n"+
"To uncouple from a car or locomotive\n"+
"1. Press F9 to open the Train Operations window.\n"+
"2. Double-click a unit in the part of the train you want to uncouple from the locomotive, to set the handbrake.\n"+
"3. Double-click the coupler between the units you want to uncouple and the rest of the train.\n"+
"4. Close the Train Operations window."
)
""",
"""
SIMISA@@@@@@@@@@JINX0t1t______

EngineOperatingProcedures (
    "To move forward\n" +
    "1.	Press Shift+E if necessary to move the Speed Gear handle to neutral.\n" +
    "2. Hold down the A key as necessary to move the Throttle to 0%.\n" +
    "3.	Press the W key to move the Reverser to Forward.\n" +
    "4. Press E to move the Speed Gear handle to 1.\n" +
    "5.	Hold down the ; (SEMICOLON) key to release the train brakes.\n" +
    "6.	Press the D key to move increase the Throttle setting.\n\n" +
    "To coast\n" +
    "1.	Hold down the A key to reduce the Throttle to 0%.\n" +
    "2.	Press Shift+E to move the Speed Gear handle from 1 to Neutral.\n\n" +
    "To resume powered operation\n" +
    "1.	If traveling under 50 km/h, press E once to move the Speed Gear handle to 1. If traveling over 50 km/h, press E twice to move the Speed Gear handle to 2.\n" +
    "2.	Press the D key to advance the Throttle.\n\n" +
    "To operate the KIHA 31 at high speed (over 50 km/h)\n" +
    "1.	Press the E key to move the Speed Gear handle to 2.\n" +
    "2.	Press the D key to advance the Throttle.\n\n" +
    "To return the KIHA 31 to low speed operation (under 50 Km/h)\n" +
    "1.	Press Shift+E to move the Speed Gear handle to 1.\n\n" +
    "To slow the train\n" +
    "1.	Press the A key to move the Throttle to a lower setting.\n" +
    "2.	Use the brakes to further reduce speed if desired.\n\n" +
    "To apply the train brakes\n" +
    "1.	Hold down the ' (APOSTROPHE) key.\n\n" +
    "To release the train brakes\n" +
    "1. Hold down the ; (SEMICOLON) key.\n\n" +
    "To operate the Engine Brake\n" +
    "1.	Press Shift+E to move the Speed Gear handle to 2.\n" +
    "2.	Hold down the A key to move the Throttle to 0%.\n" +
    "3.	Press the P key to activate the Engine Brake.\n\n" +
    "To deactivate the Engine Brake\n" +
    "1.	Press the D key to advance the Throttle.\n\n" +
    "To back up\n" +
    "1.	Bring your train to a full stop.\n" +
    "2. Hold down the A key to move the Throttle to 0%.\n" +
    "3. Press Shift+E to move the Speed Gear handle to Neutral.\n" +
    "4.	Press the S key twice to move the Reverser to the Reverse position.\n" +
    "5.	Press the ; [SEMICOLON] key to release the brakes.\n" +
    "6. Press E to move the Speed Gear handle to 1.\n" +
    "7.	Press the D key to slowly advance the Throttle.\n\n" +
    "To load and unload passengers\n" +
    "1. Stop at a station.\n" +
    "2. Press the Enter key.\n" +
    "3. Wait for the \"permission to depart\" buzzer.\n" +
    "TIP: Use the Next Station driving aid (F10) to monitor loading and unloading.\n\n" +
    "To couple to a car or locomotive\n" +
    "1. Coast into the unit at a very slow speed, about 4 km/h.\n" +
    "Tip: To use the Coupling view, press the 6 key.\n" +
    "2. Press F9 to open the Train Operations window.\n" +
    "3. If the icon for the unit you coupled to is red, the handbrake is on. Double-click the car to release the handbrake.\n" +
    "4. Close the Train Operations window.\n\n" +
    "To uncouple from a car or locomotive\n" +
    "1. Press F9 to open the Train Operations window.\n" +
    "2. Double-click a unit in the part of the train you want to uncouple from the locomotive, to set the handbrake.\n" +
    "3. Double-click the coupler between the units you want to uncouple and the rest of the train.\n" +
    "4. Close the Train Operations window."
)

""",
            new WriterOptions{ IndentSize = 4, IndentCharacter = ' ', ValuesInColumns = new Dictionary<string, int> { { "EngineOperatingProcedures", 2 } } }
        };

        yield return new object[]
        {
"""
SIMISA@@@@@@@@@@JINX0t1t______

ActivityObject (
	ObjectType ( WagonsList )
	Train_Config (
		TrainCfg ( ""
			Name ( "Loose consist." )
			Serial ( 16 )
			MaxVelocity ( 33.33130 1.00000 )
			NextWagonUID ( 8 )
			Durability ( 1.00000 )
			Engine (
				UiD ( 0 )
				EngineData ( 2000 Series2000 )
			)
			Wagon (
				WagonData ( 2000ACarriage Series2000 )
				UiD ( 1 )
			)
			Wagon (
				WagonData ( 2000ACarriage Series2000 )
				UiD ( 2 )
			)
			Wagon (
				WagonData ( 2000BCarriage Series2000 )
				UiD ( 3 )
			)
			Wagon (
				WagonData ( 2000BCarriage Series2000 )
				UiD ( 4 )
			)
			Wagon (
				WagonData ( 2000ACarriage Series2000 )
				UiD ( 5 )
			)
			Wagon (
				WagonData ( 2000BCarriage Series2000 )
				UiD ( 6 )
			)
			Engine (
				UiD ( 7 )
				EngineData ( 2000RearEng Series2000 )
			)
		)
	)
	Direction ( 0 )
	ID ( 32768 )
	Tile ( -144 14061 276.697 866.383 )
)
""",
"""
SIMISA@@@@@@@@@@JINX0t1t______

ActivityObject (
	ObjectType ( WagonsList )
	Train_Config (
		TrainCfg ( ""
			Name ( "Loose consist." )
			Serial ( 16 )
			MaxVelocity ( 33.33130 1.00000 )
			NextWagonUID ( 8 )
			Durability ( 1.00000 )
			Engine (
				UiD ( 0 )
				EngineData ( 2000 Series2000 )
			)
			Wagon (
				WagonData ( 2000ACarriage Series2000 )
				UiD ( 1 )
			)
			Wagon (
				WagonData ( 2000ACarriage Series2000 )
				UiD ( 2 )
			)
			Wagon (
				WagonData ( 2000BCarriage Series2000 )
				UiD ( 3 )
			)
			Wagon (
				WagonData ( 2000BCarriage Series2000 )
				UiD ( 4 )
			)
			Wagon (
				WagonData ( 2000ACarriage Series2000 )
				UiD ( 5 )
			)
			Wagon (
				WagonData ( 2000BCarriage Series2000 )
				UiD ( 6 )
			)
			Engine (
				UiD ( 7 )
				EngineData ( 2000RearEng Series2000 )
			)
		)
	)
	Direction ( 0 )
	ID ( 32768 )
	Tile ( -144 14061 276.697 866.383 )
)

"""
        };

        yield return new object[]
        {
"""
SIMISA@@@@@@@@@@JINX0D0t______

Wagon ( US2BNSFCar  
	comment(Gondola Car)
	Type ( Freight )
	WagonShape ( US2BNSFCar.s )
	Size ( 3.1m 3.77019m 22m)
	comment( 28.03t empty, 160.747t full )
	Mass ( "28.03t" )
	WheelRadius ( 36in/2 )
	InertiaTensor ( Box (3.1m 2.7m 22m) )
	Coupling (
		Type ( Automatic )
		Spring (
			Stiffness ( 1e6N/m 5e6N/m )
			Damping ( 1e6N/m 1e6N/m )
			Break ( 1.1e7N 1.1e7N )
			r0 ( 20cm 30cm )
		)
		Velocity ( 0.1m/s )
	)
	Buffers (
		Spring (
			Stiffness ( 1e6N/m 5e6N/m )
			Damping ( 1e6N/m/s 1e6N/m/s )
			r0 ( 0m 1e9 )
		)
		Centre ( 0.5 )
		Radius ( 1 )
		Angle ( 0.5deg )
	)

	Adheasion ( 0.2 0.4 2 0 )
	DerailRailHeight ( 4cm )
	DerailRailForce ( 2.5*28t )
	DerailBufferForce ( 400kN )
	NumWheels (	8 )
	Friction (
		100N/m/s		1		-1mph		0		1
		5.1N/rad/s		1		-1rad/s		0		1
	)

	Lights	(	2
		Light	(
			comment( Rear red light )
			Type		( 0 )
			Conditions	(
				Headlight ( 3 )
				Unit ( 3 )
				)
			FadeIn	( 0.5 )
			FadeOut	( 0.5 )
			Cycle	( 0 )
			States	(	2
				State	(
					Duration ( 0.35 )
					LightColour ( 80ff0000 )
					Position ( 0.0 3.26 -10.542 )
					Azimuth ( -180 -180 -180 )
					Transition ( 0 )
					Radius ( 1.0 )
				)
				State	(
					Duration ( 0.35 )
					LightColour ( 00000000 )
					Position ( 0.0 3.26 -10.542 )
					Azimuth ( -180 -180 -180 )
					Transition ( 0 )
					Radius ( 1.0 )
					)
				)
			)
		Light	(
			comment( Rear red light dim )
			Type		( 0 )
			Conditions	(
				Headlight ( 2 )
				Unit ( 3 )
				)
			FadeIn	( 0.5 )
			FadeOut	( 0.5 )
			Cycle	( 0 )
			States	(	2
				State	(
					Duration ( 0.35 )
					LightColour ( 40ff0000 )
					Position ( 0.0 3.26 -10.542 )
					Azimuth ( -180 -180 -180 )
					Transition ( 0 )
					Radius ( 1.0 )
				)
				State	(
					Duration ( 0.35 )
					LightColour ( 00000000 )
					Position ( 0.0 3.26 -10.542 )
					Azimuth ( -180 -180 -180 )
					Transition ( 0 )
					Radius ( 1.0 )
		   		)
			)
		)
	)

	BrakeEquipmentType( "Handbrake, Triple_valve, Auxilary_reservoir, Emergency_brake_reservoir" )
	BrakeSystemType( "Air_single_pipe" )
	MaxBrakeForce( 50kN )

	MaxHandbrakeForce( 35kN )
	NumberOfHandbrakeLeverSteps( 100 )

	TripleValveRatio( 2.5 )
	MaxReleaseRate( 15 )
	MaxApplicationRate( 25 )
	MaxAuxilaryChargingRate( 5 )
	EmergencyResCapacity( 6 )
	EmergencyResChargingRate( 5 )
	BrakeCylinderPressureForMaxBrakeBrakeForce( 70 )
	EmergencyBrakeResMaxPressure( 90 )

  	Sound (	"GenFreightWag1.sms" )
)

""",
"""
SIMISA@@@@@@@@@@JINX0D0t______

Wagon ( US2BNSFCar
	comment (Gondola Car)
	Type ( Freight )
	WagonShape ( US2BNSFCar.s )
	Size ( 3.1m 3.77019m 22m )
	comment ( 28.03t empty, 160.747t full )
	Mass ( "28.03t" )
	WheelRadius ( 36in/2 )
	InertiaTensor ( Box ( 3.1m 2.7m 22m ) )
	Coupling (
		Type ( Automatic )
		Spring (
			Stiffness ( 1e6N/m 5e6N/m )
			Damping ( 1e6N/m 1e6N/m )
			Break ( 1.1e7N 1.1e7N )
			r0 ( 20cm 30cm )
		)
		Velocity ( 0.1m/s )
	)
	Buffers (
		Spring (
			Stiffness ( 1e6N/m 5e6N/m )
			Damping ( 1e6N/m/s 1e6N/m/s )
			r0 ( 0m 1e9 )
		)
		Centre ( 0.5 )
		Radius ( 1 )
		Angle ( 0.5deg )
	)

	Adheasion ( 0.2 0.4 2 0 )
	DerailRailHeight ( 4cm )
	DerailRailForce ( 2.5*28t )
	DerailBufferForce ( 400kN )
	NumWheels ( 8 )
	Friction (
		100N/m/s 1 -1mph 0 1
		5.1N/rad/s 1 -1rad/s 0 1
	)

	Lights ( 2
		Light (
			comment ( Rear red light )
			Type ( 0 )
			Conditions ( Headlight ( 3 ) Unit ( 3 ) )
			FadeIn ( 0.5 )
			FadeOut ( 0.5 )
			Cycle ( 0 )
			States ( 2
				State ( Duration ( 0.35 ) LightColour ( 80ff0000 ) Position ( 0.0 3.26 -10.542 ) Azimuth ( -180 -180 -180 ) Transition ( 0 ) Radius ( 1.0 ) )
				State ( Duration ( 0.35 ) LightColour ( 00000000 ) Position ( 0.0 3.26 -10.542 ) Azimuth ( -180 -180 -180 ) Transition ( 0 ) Radius ( 1.0 ) )
			)
		)
		Light (
			comment ( Rear red light dim )
			Type ( 0 )
			Conditions ( Headlight ( 2 ) Unit ( 3 ) )
			FadeIn ( 0.5 )
			FadeOut ( 0.5 )
			Cycle ( 0 )
			States ( 2
				State ( Duration ( 0.35 ) LightColour ( 40ff0000 ) Position ( 0.0 3.26 -10.542 ) Azimuth ( -180 -180 -180 ) Transition ( 0 ) Radius ( 1.0 ) )
				State ( Duration ( 0.35 ) LightColour ( 00000000 ) Position ( 0.0 3.26 -10.542 ) Azimuth ( -180 -180 -180 ) Transition ( 0 ) Radius ( 1.0 ) )
			)
		)
	)

	BrakeEquipmentType ( "Handbrake, Triple_valve, Auxilary_reservoir, Emergency_brake_reservoir" )
	BrakeSystemType ( "Air_single_pipe" )
	MaxBrakeForce ( 50kN )

	MaxHandbrakeForce ( 35kN )
	NumberOfHandbrakeLeverSteps ( 100 )

	TripleValveRatio ( 2.5 )
	MaxReleaseRate ( 15 )
	MaxApplicationRate ( 25 )
	MaxAuxilaryChargingRate ( 5 )
	EmergencyResCapacity ( 6 )
	EmergencyResChargingRate ( 5 )
	BrakeCylinderPressureForMaxBrakeBrakeForce ( 70 )
	EmergencyBrakeResMaxPressure ( 90 )

	Sound ( "GenFreightWag1.sms" )
)

""",
            new WriterOptions{ Inline = ["InertiaTensor", "Conditions", "State"], ValuesInColumns = new Dictionary<string, int> { { "Friction", 5 } }}
        };
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
