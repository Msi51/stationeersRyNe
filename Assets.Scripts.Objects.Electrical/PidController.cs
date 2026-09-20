using System;

namespace Assets.Scripts.Objects.Electrical;

public class PidController
{
	private double Ts;

	private double K;

	private double b0;

	private double b1;

	private double b2;

	private double a0;

	private double a1;

	private double a2;

	private double y0;

	private double y1;

	private double y2;

	private double e0;

	private double e1;

	private double e2;

	public double Kp { get; set; }

	public double Ki { get; set; }

	public double Kd { get; set; }

	public double N { get; set; }

	public double TsMin { get; set; } = 0.001;

	public double OutputUpperLimit { get; set; }

	public double OutputLowerLimit { get; set; }

	public PidController(double kp, double ki, double kd, double n, double outputUpperLimit, double outputLowerLimit)
	{
		Kp = kp;
		Ki = ki;
		Kd = kd;
		N = n;
		OutputUpperLimit = outputUpperLimit;
		OutputLowerLimit = outputLowerLimit;
	}

	public double Iterate(double setPoint, double processValue, TimeSpan ts)
	{
		Ts = ((ts.TotalSeconds >= TsMin) ? ts.TotalSeconds : TsMin);
		K = 2.0 / Ts;
		b0 = Math.Pow(K, 2.0) * Kp + K * Ki + Ki * N + K * Kp * N + Math.Pow(K, 2.0) * Kd * N;
		b1 = 2.0 * Ki * N - 2.0 * Math.Pow(K, 2.0) * Kp - 2.0 * Math.Pow(K, 2.0) * Kd * N;
		b2 = Math.Pow(K, 2.0) * Kp - K * Ki + Ki * N - K * Kp * N + Math.Pow(K, 2.0) * Kd * N;
		a0 = Math.Pow(K, 2.0) + N * K;
		a1 = -2.0 * Math.Pow(K, 2.0);
		a2 = Math.Pow(K, 2.0) - K * N;
		e2 = e1;
		e1 = e0;
		e0 = setPoint - processValue;
		y2 = y1;
		y1 = y0;
		y0 = (0.0 - a1) / a0 * y1 - a2 / a0 * y2 + b0 / a0 * e0 + b1 / a0 * e1 + b2 / a0 * e2;
		if (double.IsNaN(y0) || double.IsInfinity(y0))
		{
			ResetController();
			return 0.0;
		}
		if (OutputUpperLimit < 3.4028234663852886E+38 && y0 > OutputUpperLimit)
		{
			y0 = OutputUpperLimit;
		}
		if (OutputLowerLimit > -3.4028234663852886E+38 && y0 < OutputLowerLimit)
		{
			y0 = OutputLowerLimit;
		}
		return y0;
	}

	public void ResetController()
	{
		e2 = 0.0;
		e1 = 0.0;
		e0 = 0.0;
		y2 = 0.0;
		y1 = 0.0;
		y0 = 0.0;
	}
}
