using CalcEngineService.Calculators;
using CalcEngineService.Messages;
using NUnit.Framework;

namespace OptionsTests;


[TestFixture]
public class OptionCalculationTests
{

    [Test]
    [TestCase(100, 100, 30, 0,  3, 1, 13.2833, 0.5987, 0.0129, 0.3867, 0.4658, -0.0197, -0.5982, 10.33)]
    public void OptionValueAndGreeksTest(double spotPx, double strike, double volPct, double divYieldPct, double ratePct, double maturityTimeYrs,
        double expectedCallOptionValue, double expectedCallDelta, double expectedCallGamma, double expectedCallVega, double expectedCallRho, double expectedCallTheta, double expectedCallDivRho,
        double expectedPutOptionValue)
    {
        ParameterSet parameterSet = new ParameterSet();
        parameterSet.SpotPx = spotPx;
        parameterSet.StrikePrice = strike;
        parameterSet.VolatilityPct = volPct;
        parameterSet.DividendYieldPct = divYieldPct;
        parameterSet.RiskFreeRatePct = ratePct;
        parameterSet.MaturityTimeYrs = maturityTimeYrs;
        parameterSet.CallValuationResults = new ValuationResults();
        parameterSet.PutValuationResults = new ValuationResults();

        OptionPriceCalculator.CalculateOptionPriceAndGreeks(parameterSet);

        Assert.IsTrue(AssertDoubleEqual(expectedCallOptionValue, parameterSet.CallValuationResults.OptionValue), $"CallOptionValue: {expectedCallOptionValue} <=> {parameterSet.CallValuationResults.OptionValue}");
        Assert.IsTrue(AssertDoubleEqual(expectedCallDelta, parameterSet.CallValuationResults.Delta), $"CallDelta: {expectedCallDelta} <=> {parameterSet.CallValuationResults.Delta}");
        Assert.IsTrue(AssertDoubleEqual(expectedCallGamma, parameterSet.CallValuationResults.Gamma), $"CallGamma: {expectedCallGamma} <=> {parameterSet.CallValuationResults.Gamma}");
        Assert.IsTrue(AssertDoubleEqual(expectedCallVega, parameterSet.CallValuationResults.Vega), $"CallVega: {expectedCallVega} <=> {parameterSet.CallValuationResults.Vega}");
        Assert.IsTrue(AssertDoubleEqual(expectedCallRho, parameterSet.CallValuationResults.Rho), $"CallRho: {expectedCallRho} <=> {parameterSet.CallValuationResults.Rho}");
        Assert.IsTrue(AssertDoubleEqual(expectedCallTheta, parameterSet.CallValuationResults.Theta), $"CallTheta: {expectedCallTheta} <=> {parameterSet.CallValuationResults.Theta}");
        Assert.IsTrue(AssertDoubleEqual(expectedCallDivRho, parameterSet.CallValuationResults.Divrho), $"CallDivRho: {expectedCallDivRho} <=> {parameterSet.CallValuationResults.Divrho}");


        Assert.IsTrue(AssertDoubleEqual(expectedPutOptionValue, parameterSet.PutValuationResults.OptionValue), $"PutOptionValue: {expectedPutOptionValue} <=> {parameterSet.PutValuationResults.OptionValue}");
    }


    private bool AssertDoubleEqual(double expected, double actual)
    {
        var epsilon = 0.01;

        return Math.Abs(expected - actual) < epsilon;
    }

}