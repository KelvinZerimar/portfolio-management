using System.Globalization;
using System.Net;
using Domain.Entities;

namespace Application.Reports;

public sealed record PortfolioMarketRow(string Symbol, string CoinGeckoId, decimal? ChangePercentage, decimal High, decimal Low);

internal static class PortfolioStatusReportEmailTemplate
{
    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("es-ES");

    public static string RenderSubject(string portfolioName, PortfolioStatusReport report)
        => $"Resumen de {portfolioName} — {report.PeriodStart.ToString("MMMM yyyy", Culture)}";

    public static string RenderBody(string portfolioName, PortfolioStatusReport report, IReadOnlyList<PortfolioMarketRow> marketRows)
    {
        var isPositive = report.ChangeAmount >= 0;
        var changeSign = isPositive ? "+" : string.Empty;
        var changeColor = isPositive ? "#1a7f37" : "#c62828";
        var changePercentageText = report.ChangePercentage is { } percentage
            ? $"{changeSign}{percentage.ToString("0.00", Culture)}%"
            : "s/d";

        var holdingsRows = string.Join(string.Empty, report.Holdings.Select(h => $"""
            <tr>
              <td style="padding:4px 8px;">{WebUtility.HtmlEncode(h.Symbol)}</td>
              <td style="padding:4px 8px; text-align:right;">{h.Quantity.ToString("0.########", Culture)}</td>
              <td style="padding:4px 8px; text-align:right;">{h.Value.ToString("C2", Culture)}</td>
            </tr>
            """));

        var marketSection = marketRows.Count == 0 ? string.Empty : $"""
            <h3 style="margin-top:24px; margin-bottom:4px;">Mercado del mes anterior</h3>
            <table style="border-collapse:collapse; width:100%;">
              <thead>
                <tr style="border-bottom:1px solid #ccc;">
                  <th style="text-align:left; padding:4px 8px;">Activo</th>
                  <th style="text-align:right; padding:4px 8px;">Variación</th>
                  <th style="text-align:right; padding:4px 8px;">Máx</th>
                  <th style="text-align:right; padding:4px 8px;">Mín</th>
                  <th style="text-align:left; padding:4px 8px;">Fuente</th>
                </tr>
              </thead>
              <tbody>{string.Join(string.Empty, marketRows.Select(RenderMarketRow))}</tbody>
            </table>
            <p style="color:#888; font-size:12px; margin-top:8px;">
              Datos históricos de mercado vía CoinGecko (enlace en cada fila). Esto es información retrospectiva, no una predicción ni una recomendación de inversión.
            </p>
            """;

        return $"""
            <html>
            <body style="font-family: Segoe UI, Arial, sans-serif; color:#1f1f1f;">
              <h2 style="margin-bottom:0;">{WebUtility.HtmlEncode(portfolioName)}</h2>
              <p style="color:#555; margin-top:4px;">
                Resumen del {report.PeriodStart.ToString("dd/MM/yyyy", Culture)} al {report.PeriodEnd.ToString("dd/MM/yyyy", Culture)}
              </p>
              <p style="font-size:24px; margin:16px 0 4px;"><strong>{report.ValueAtPeriodEnd.ToString("C2", Culture)}</strong></p>
              <p style="color:{changeColor}; margin-top:0;">
                {changeSign}{report.ChangeAmount.ToString("C2", Culture)} ({changePercentageText}) respecto al inicio del período
              </p>
              <table style="border-collapse:collapse; width:100%; margin-top:16px;">
                <thead>
                  <tr style="border-bottom:1px solid #ccc;">
                    <th style="text-align:left; padding:4px 8px;">Activo</th>
                    <th style="text-align:right; padding:4px 8px;">Cantidad</th>
                    <th style="text-align:right; padding:4px 8px;">Valor</th>
                  </tr>
                </thead>
                <tbody>{holdingsRows}</tbody>
              </table>
              {marketSection}
              <p style="color:#888; font-size:12px; margin-top:24px;">
                Informe generado automáticamente a partir de tus registros manuales de cartera. No constituye asesoramiento financiero.
              </p>
            </body>
            </html>
            """;
    }

    private static string RenderMarketRow(PortfolioMarketRow row)
    {
        var changeText = row.ChangePercentage is { } percentage
            ? $"{(percentage >= 0 ? "+" : string.Empty)}{percentage.ToString("0.00", Culture)}%"
            : "s/d";
        var changeColor = row.ChangePercentage is { } p && p < 0 ? "#c62828" : "#1a7f37";
        var sourceUrl = $"https://www.coingecko.com/en/coins/{Uri.EscapeDataString(row.CoinGeckoId)}";

        return $"""
            <tr>
              <td style="padding:4px 8px;">{WebUtility.HtmlEncode(row.Symbol)}</td>
              <td style="padding:4px 8px; text-align:right; color:{changeColor};">{changeText}</td>
              <td style="padding:4px 8px; text-align:right;">{row.High.ToString("C2", Culture)}</td>
              <td style="padding:4px 8px; text-align:right;">{row.Low.ToString("C2", Culture)}</td>
              <td style="padding:4px 8px;"><a href="{sourceUrl}">CoinGecko</a></td>
            </tr>
            """;
    }
}
