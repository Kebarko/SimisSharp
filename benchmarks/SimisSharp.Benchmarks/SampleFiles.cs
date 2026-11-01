using System.Globalization;
using System.Text;

namespace KE.MSTS.SimisSharp.Benchmarks;

/// <summary>
/// Generates SIMIS text that resembles real files, so the benchmarks need no MSTS installation.
/// </summary>
public static class SampleFiles
{
    /// <summary>
    /// A world file with <paramref name="objects"/> static objects: many small blocks of numbers and a quoted file name each.
    /// </summary>
    public static string World(int objects)
    {
        StringBuilder sb = new("SIMISA@@@@@@@@@@JINX0w0t______\r\n\r\nTr_Worldfile (\r\n");
        for (int i = 0; i < objects; i++)
        {
            double x = -1024 + (i * 7.31 % 2048);
            double z = 1024 - (i * 3.17 % 2048);
            sb.Append(string.Create(CultureInfo.InvariantCulture, $"""
                	Static (
                		UiD ( {i} )
                		FileName ( "tree{i % 50}.s" )
                		StaticFlags ( 00200100 )
                		Position ( {x:F3} 12.5 {z:F3} )
                		QDirection ( 0 0.707107 0 0.707107 )
                		VDbId ( 4294967295 )
                		StaticDetailLevel ( 2 )
                	)

                """).ReplaceLineEndings("\r\n"));
        }

        sb.Append(")\r\n");
        return sb.ToString();
    }

    /// <summary>
    /// An engine file with comments, quoted strings, numbers with units, tables and nested blocks.
    /// </summary>
    public static string Engine()
    {
        StringBuilder sb = new("SIMISA@@@@@@@@@@JINX0D0t______\r\n\r\n");
        sb.Append("""
            Wagon ( Bench_Loco
            	comment ( Generated for benchmarks. "Quoted (text)" and ( nested ) blocks are skipped. )
            	Type ( Engine )
            	WagonShape ( bench.s )
            	Size ( 3.1m 4.65m 19.2m )
            	Mass ( 88t )
            	WheelRadius ( 0.625m )
            	InertiaTensor ( Box ( 3.1m 4.65m 19.2m ) )
            	Coupling (
            		Type ( Automatic )
            		Spring ( Stiffness ( 1e6N/m 5e6N/m ) Damping ( 1e6N/m/s 1e6N/m/s ) Break ( 1e7N 1e7N ) r0 ( 0cm 3cm ) )
            		Velocity ( 0.1m/s )
            	)
            	Buffers ( Spring ( Stiffness ( 1e6N/m 5e6N/m ) Damping ( 1e6N/m/s 1e6N/m/s ) r0 ( 0m 1e9 ) ) Centre ( 0.5 ) Radius ( 1 ) Angle ( 0.5deg ) )
            	Adheasion ( 0.2 0.4 2.0 0.0 )
            	DerailRailHeight ( 4cm )
            	DerailRailForce ( 2.5m/s^2*88t )
            	DerailBufferForce ( 400kN )
            	NumWheels ( 4 )
            	Friction (
            		5.1N/m/s 0 1mph 0 1
            		5.1N/m/s 0 1mph 0 1
            	)
            	BrakeEquipmentType ( "Handbrake, Triple_valve, Auxilary_reservoir, Emergency_brake_reservoir" )
            	BrakeSystemType ( "Air_twin_pipe" )
            	MaxBrakeForce ( 160kN )
            	Lights ( 4
            """.ReplaceLineEndings("\r\n"));

        for (int i = 0; i < 4; i++)
        {
            sb.Append(string.Create(CultureInfo.InvariantCulture, $"""

                		Light (
                			comment ( Head light {i} )
                			Type ( 0 ) Conditions ( Headlight ( 3 ) Unit ( 2 ) )
                			Cycle ( 0 ) FadeIn ( 0.5 ) FadeOut ( 0.5 )
                			States ( 1 State ( Duration ( 0.0 ) LightColour ( ffffffff ) Position ( {i * 0.4:F1} 3.2 9.5 ) Azimuth ( 0 0 0 ) Transition ( 0 ) Radius ( 0.5 ) ) )
                		)
                """).ReplaceLineEndings("\r\n"));
        }

        sb.Append("""

            	)
            	Sound ( "bench_engine.sms" )
            )

            Engine ( Bench_Loco
            	Wagon ( Bench_Loco )
            	Type ( Electric )
            	MaxPower ( 3000kW )
            	MaxForce ( 280kN )
            	MaxContinuousForce ( 240kN )
            	MaxVelocity ( 120km/h )
            	#ORTSMaxTractiveForceCurves ( comment ( ignored by MSTS ) )
            	Sanding ( 30mph )
            	MaxCurrent ( 1000A )
            	EngineControllers (
            		Throttle ( 0 1 0.1 0
            			NumNotches ( 0 )
            		)
            		Brake_Train ( 0 1 0.0125 0.5
            			NumNotches ( 4
            				Notch ( 0 0 TrainBrakesControllerReleaseStart )
            				Notch ( 0.25 0 TrainBrakesControllerRunningStart )
            				Notch ( 0.5 1 TrainBrakesControllerGraduatedSelfLapLimitedStart )
            				Notch ( 1 0 TrainBrakesControllerEmergencyStart )
            			)
            		)
            	)
            	Name ( "Bench locomotive" )
            	Description ( "A locomotive generated for benchmarks.\nIt has no real counterpart." )
            )

            """.ReplaceLineEndings("\r\n"));

        return sb.ToString();
    }
}
