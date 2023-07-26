using CalcEngineService.Messages;

namespace CalcEngineService.Calculators;

public static class OptionPriceCalculator
{
    public static void CalculateOptionPriceAndGreeks(ParameterSet paramSet)
    {
        var (callOptionValue, putOptionValue) =
            CalculateOptionPrice(paramSet.SpotPx, paramSet.StrikePrice, paramSet.RiskFreeRatePct, 
            paramSet.DividendYieldPct, paramSet.VolatilityPct, paramSet.MaturityTimeYrs);

        var epsilon = 0.01;

        //delta
        var (newCallOptionValue1, newPutOptionValue1) =
            CalculateOptionPrice(paramSet.SpotPx + epsilon, paramSet.StrikePrice, paramSet.RiskFreeRatePct,
            paramSet.DividendYieldPct, paramSet.VolatilityPct, paramSet.MaturityTimeYrs);

        var deltaCallOption1 = (newCallOptionValue1 - callOptionValue) / epsilon;
        var deltaPutOption1 = (newPutOptionValue1 - putOptionValue) / epsilon;

        //gamma
        var newSpotPx = 1.1 * paramSet.SpotPx;
        var (newCallOptionValue1_1, newPutOptionValue1_1) =
            CalculateOptionPrice(newSpotPx, paramSet.StrikePrice, paramSet.RiskFreeRatePct,
            paramSet.DividendYieldPct, paramSet.VolatilityPct, paramSet.MaturityTimeYrs);

        //delta 2
        var (newCallOptionValue1_2, newPutOptionValue1_2) =
            CalculateOptionPrice(newSpotPx + epsilon, paramSet.StrikePrice, paramSet.RiskFreeRatePct,
            paramSet.DividendYieldPct, paramSet.VolatilityPct, paramSet.MaturityTimeYrs);

        var deltaCallOption1_1 = (newCallOptionValue1_2 - newCallOptionValue1_1) / epsilon;
        var deltaPutOption1_1 = (newPutOptionValue1_2 - newPutOptionValue1_1) / epsilon;

        var gammaCallOption = (deltaCallOption1_1 - deltaCallOption1) / (newSpotPx - paramSet.SpotPx);
        var gammaPutOption = (deltaPutOption1_1 - deltaPutOption1) / (newSpotPx - paramSet.SpotPx);

        //vega
        var (newCallOptionValue3, newPutOptionValue3) =
            CalculateOptionPrice(paramSet.SpotPx, paramSet.StrikePrice, paramSet.RiskFreeRatePct,
            paramSet.DividendYieldPct, paramSet.VolatilityPct + epsilon, paramSet.MaturityTimeYrs);

        var vegaCallOption = (newCallOptionValue3 - callOptionValue) / epsilon;
        var vegaPutOption = (newPutOptionValue3 - putOptionValue) / epsilon;

        //rho
        var (newCallOptionValue4, newPutOptionValue4) =
            CalculateOptionPrice(paramSet.SpotPx, paramSet.StrikePrice, paramSet.RiskFreeRatePct + epsilon,
            paramSet.DividendYieldPct, paramSet.VolatilityPct, paramSet.MaturityTimeYrs);

        var rhoCallOption = (newCallOptionValue4 - callOptionValue) / epsilon;
        var rhoPutOption = (newPutOptionValue4 - putOptionValue) / epsilon;

        //theta
        var (newCallOptionValue5, newPutOptionValue5) =
            CalculateOptionPrice(paramSet.SpotPx, paramSet.StrikePrice, paramSet.RiskFreeRatePct,
            paramSet.DividendYieldPct, paramSet.VolatilityPct, paramSet.MaturityTimeYrs + epsilon);

        var thetaCallOption = (callOptionValue - newCallOptionValue5) / (epsilon * 365);
        var thetaPutOption = (putOptionValue - newPutOptionValue5) / (epsilon * 365);

        //divRho
        var (newCallOptionValue6, newPutOptionValue6) =
            CalculateOptionPrice(paramSet.SpotPx, paramSet.StrikePrice, paramSet.RiskFreeRatePct,
            paramSet.DividendYieldPct + epsilon, paramSet.VolatilityPct, paramSet.MaturityTimeYrs);

        var divRhoCallOption = (newCallOptionValue6 - callOptionValue) / epsilon;
        var divRhoPutOption = (newPutOptionValue6 - putOptionValue) / epsilon;


        paramSet.CallValuationResults.OptionValue = callOptionValue;
        paramSet.CallValuationResults.Delta = deltaCallOption1;
        paramSet.CallValuationResults.Gamma = gammaCallOption;
        paramSet.CallValuationResults.Vega = vegaCallOption;
        paramSet.CallValuationResults.Rho = rhoCallOption;
        paramSet.CallValuationResults.Theta = thetaCallOption;
        paramSet.CallValuationResults.Divrho= divRhoCallOption;

        paramSet.PutValuationResults.OptionValue = putOptionValue;
        paramSet.PutValuationResults.Delta = deltaPutOption1;
        paramSet.PutValuationResults.Gamma = gammaPutOption;
        paramSet.PutValuationResults.Vega = vegaPutOption;
        paramSet.PutValuationResults.Rho = rhoPutOption;
        paramSet.PutValuationResults.Theta = thetaPutOption;
        paramSet.PutValuationResults.Divrho = divRhoPutOption;
    }

    private static (double, double) CalculateOptionPrice(double spotPx, double strikePrice, double riskFreeRatePct,
        double dividendYieldPct, double volatilityPct, double maturityTimeYrs)
    {
        var logSpotOverStrike = GetSpotOverStrike(spotPx, strikePrice);
        var rateMinusDivYield = GetRateMinusDivYield(riskFreeRatePct, dividendYieldPct);
        var halfVolSquare = GetHalfVolSquare(volatilityPct);
        var volSqrtTime = GetVolSqrtTime(volatilityPct, maturityTimeYrs);
        var expDivYieldMaturity = GetExpDivYieldMaturity(dividendYieldPct, maturityTimeYrs);
        var expRateMaturity = GetExpRateMaturity(riskFreeRatePct, maturityTimeYrs);

        var d1 = (logSpotOverStrike + (rateMinusDivYield + halfVolSquare) * maturityTimeYrs) / volSqrtTime;
        var d2 = d1 - volSqrtTime;

        var callOptionValue = spotPx * expDivYieldMaturity * CND(d1) - strikePrice * expRateMaturity * CND(d2);
        var putOptionValue = strikePrice * expRateMaturity * CND(-1 * d2) - spotPx * expDivYieldMaturity * CND(-1 * d1);

        return (callOptionValue, putOptionValue);
    }

    private static double GetSpotOverStrike(double spot, double strike) 
        => Math.Log(spot / strike);

    private static double GetRateMinusDivYield(double riskFreeRatePct, double dividendYieldPct) 
        => (riskFreeRatePct - dividendYieldPct) / 100.0;

    private static double GetHalfVolSquare(double volatilityPct)
        => (volatilityPct * volatilityPct) / (2.0 * 10000.0);

    private static double GetVolSqrtTime(double volatilityPct, double maturityTimeYrs)
        => volatilityPct / 100.0 * Math.Sqrt(maturityTimeYrs);

    private static double GetExpDivYieldMaturity(double dividendYieldPct, double maturityTimeYrs)
        => Math.Exp(-1 * dividendYieldPct / 100.0 * maturityTimeYrs);

    private static double GetExpRateMaturity(double riskFreeRatePct, double maturityTimeYrs)
        => Math.Exp(-1 * riskFreeRatePct / 100.0 * maturityTimeYrs);


    // Abromowitz and Stegun approximation
    private static double CND(double z)
    {
        double p = 0.3275911;
        double a1 = 0.254829592;
        double a2 = -0.284496736;
        double a3 = 1.421413741;
        double a4 = -1.453152027;
        double a5 = 1.061405429;

        int sign;
        if (z < 0.0)
            sign = -1;
        else
            sign = 1;

        double x = Math.Abs(z) / Math.Sqrt(2.0);
        double t = 1.0 / (1.0 + p * x);
        double erf = 1.0 - (((((a5 * t + a4) * t) + a3)
          * t + a2) * t + a1) * t * Math.Exp(-x * x);
        return 0.5 * (1.0 + sign * erf);
    }
}
