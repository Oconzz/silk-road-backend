namespace Business.Templates;

public static class EstimateHtmlTemplate
{
    public static string FormatCurrency(decimal amount) => $"${amount:N2}";
    
    public static string FormatDate(DateTime? date) => date.HasValue 
        ? date.Value.ToString("MMMM d, yyyy") 
        : "N/A";

    public static string Generate(
        string companyName,
        string estimateNumber,
        DateTime createDate,
        DateTime? expirationDate,
        decimal total,
        string? clientName,
        string? clientEmail,
        string? clientPhone,
        string? locationAddress,
        string? locationCityState,
        string? description,
        string lineItemsHtml,
        bool sent = false,
        bool approved = false,
        string shareId = "",
        DateTime? proposedStartDate = null)
    {
        // Status badge (Square-style: Sent, Not Approved, Approved)
        string statusBadgeClass, statusBadgeText;
        if (approved)
        {
            statusBadgeClass = "status-approved";
            statusBadgeText = "Approved";
        }
        else if (sent)
        {
            statusBadgeClass = "status-sent";
            statusBadgeText = "Sent";
        }
        else
        {
            statusBadgeClass = "status-draft";
            statusBadgeText = "Draft";
        }

        // Approve button section (only show if not yet approved and has shareId)
        var approveButtonHtml = "";
        if (!approved && !string.IsNullOrEmpty(shareId))
        {
            approveButtonHtml = $@"
                <div class=""approve-section"">
                    <div class=""approve-card"">
                        <div class=""approve-icon"">✓</div>
                        <div class=""approve-title"">Ready to proceed?</div>
                        <div class=""approve-subtitle"">By approving, you agree to the scope and pricing outlined in this estimate.</div>
                        <button id=""approveBtn"" class=""approve-button"" onclick=""approveEstimate()"">
                            Approve Estimate
                        </button>
                    </div>
                </div>";
        }
        else if (approved)
        {
            approveButtonHtml = @"
                <div class=""approve-section"">
                    <div class=""approved-card"">
                        <div class=""approved-icon"">✓</div>
                        <div class=""approved-message"">Your Estimate Has Been Approved</div>
                        <div class=""approved-next-steps"">We Will Be In Contact With You Shortly.</div>
                    </div>
                </div>";
        }

        var clientSectionHtml = "";
        if (!string.IsNullOrEmpty(clientName))
        {
            var locationBoxHtml = "";
            if (!string.IsNullOrEmpty(locationAddress))
            {
                locationBoxHtml = $@"
                    <div class=""client-box"">
                        <div class=""section-label"">SERVICE LOCATION</div>
                        <div class=""client-name"">{locationAddress}</div>
                        <div class=""client-details"">{locationCityState}</div>
                    </div>";
            }

            clientSectionHtml = $@"
                <div class=""client-section"">
                    <div class=""client-box"">
                        <div class=""section-label"">ADDRESS TO</div>
                        <div class=""client-name"">{clientName}</div>
                        {(!string.IsNullOrEmpty(clientEmail) ? $@"<div class=""client-details"">{clientEmail}</div>" : "")}
                        {(!string.IsNullOrEmpty(clientPhone) ? $@"<div class=""client-details"">{clientPhone}</div>" : "")}
                    </div>
                    {locationBoxHtml}
                </div>";
        }

        var validityNoticeHtml = expirationDate.HasValue ? $@"
            <div class=""validity-notice"">
                This estimate is valid until <strong>{FormatDate(expirationDate)}</strong>
            </div>" : "";

        var descriptionHtml = !string.IsNullOrEmpty(description) ? $@"
            <div class=""description-box"">
                <div class=""section-label"">PROJECT DESCRIPTION</div>
                <div class=""description-text"">{description}</div>
            </div>" : "";

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
    <title>Estimate {estimateNumber}</title>
    <style>
        * {{ margin: 0; padding: 0; box-sizing: border-box; }}
        
        body {{
            font-family: 'Helvetica Neue', Helvetica, Arial, sans-serif;
            color: #1A1A1A;
            background: #fff;
            line-height: 1.5;
            font-size: 14px;
        }}
        
        .document {{ max-width: 800px; margin: 0 auto; background: #fff; }}
        
        .header {{
            background: linear-gradient(135deg, #004732 0%, #006644 100%);
            color: white;
            padding: 40px;
        }}
        
        .header-content {{
            display: flex;
            flex-direction: column;
            align-items: flex-start;
            gap: 15px;
        }}
        
        .brand-name {{
            font-size: 32px;
            font-weight: 700;
            letter-spacing: 2px;
            color: #D1FF9B;
        }}
        
        .company-name {{
            font-size: 24px;
            font-weight: 600;
            color: white;
        }}
        
        .estimate-badge {{
            background: #D1FF9B;
            color: #004732;
            padding: 8px 20px;
            border-radius: 30px;
            font-weight: 600;
            font-size: 13px;
            text-transform: uppercase;
            letter-spacing: 1px;
        }}
        
        .info-bar {{
            background: #FAFAFA;
            padding: 25px 40px;
            display: flex;
            justify-content: space-between;
            border-bottom: 1px solid #E5E5E5;
            flex-wrap: wrap;
            gap: 15px;
        }}
        
        .info-item {{ text-align: center; min-width: 120px; }}
        .info-label {{ font-size: 11px; color: #666666; text-transform: uppercase; letter-spacing: 1px; margin-bottom: 5px; }}
        .info-value {{ font-size: 16px; font-weight: 600; color: #004732; }}
        
        .content {{ padding: 40px; }}
        
        .client-section {{ display: flex; gap: 40px; margin-bottom: 35px; flex-wrap: wrap; }}
        
        .client-box {{
            flex: 1;
            min-width: 250px;
            background: #FAFAFA;
            border-radius: 12px;
            padding: 20px;
            border-left: 4px solid #004732;
        }}
        
        .section-label {{ font-size: 11px; color: #666666; text-transform: uppercase; letter-spacing: 1px; margin-bottom: 10px; }}
        .client-name {{ font-size: 18px; font-weight: 600; color: #1A1A1A; margin-bottom: 5px; }}
        .client-details {{ font-size: 14px; color: #666666; line-height: 1.6; }}
        
        .description-box {{
            background: #f8f8f8;
            border-radius: 12px;
            padding: 20px;
            margin-bottom: 35px;
            border: 1px solid #E5E5E5;
        }}
        .description-text {{ font-size: 14px; color: #1A1A1A; line-height: 1.7; }}
        
        .validity-notice {{
            background: #FFF8E6;
            border: 1px solid #D1FF9B;
            border-radius: 8px;
            padding: 12px 20px;
            margin-bottom: 25px;
            font-size: 13px;
            color: #1A1A1A;
        }}
        
        .items-table {{
            width: 100%;
            border-collapse: collapse;
            margin-bottom: 30px;
            border-radius: 12px;
            overflow: hidden;
            box-shadow: 0 2px 8px rgba(0,0,0,0.06);
        }}
        
        .items-table thead {{ background: #004732; color: white; }}
        .items-table th {{
            padding: 15px 12px;
            text-align: left;
            font-size: 12px;
            font-weight: 600;
            text-transform: uppercase;
            letter-spacing: 0.5px;
        }}
        .items-table th.center {{ text-align: center; }}
        .items-table th.right {{ text-align: right; }}
        
        .cell {{ padding: 15px 12px; border-bottom: 1px solid #E5E5E5; vertical-align: top; }}
        .cell-num {{ width: 50px; color: #666666; font-weight: 500; }}
        .cell-desc {{ font-weight: 500; }}
        .cell-center {{ text-align: center; }}
        .cell-right {{ text-align: right; }}
        .cell-total {{ font-weight: 600; color: #004732; }}
        
        .item-media {{ display: flex; gap: 8px; margin-top: 8px; flex-wrap: wrap; }}
        .item-thumb {{ width: 60px; height: 60px; border-radius: 6px; object-fit: cover; border: 1px solid #E5E5E5; }}
        
        .row-even {{ background: #fff; }}
        .row-odd {{ background: #FAFAFA; }}
        
        .totals-wrapper {{ display: flex; justify-content: flex-end; margin-bottom: 40px; }}
        .totals-box {{
            width: 300px;
            background: #FAFAFA;
            border-radius: 12px;
            overflow: hidden;
            box-shadow: 0 2px 8px rgba(0,0,0,0.06);
        }}
        
        .total-row {{
            display: flex;
            justify-content: space-between;
            padding: 15px 20px;
            border-bottom: 1px solid #E5E5E5;
        }}
        .total-row:last-child {{ border-bottom: none; }}
        .total-label {{ color: #666666; font-size: 14px; }}
        .total-value {{ font-weight: 600; color: #1A1A1A; }}
        
        .grand-total {{ background: #004732; color: white; }}
        .grand-total .total-label {{ color: white; font-weight: 600; font-size: 16px; }}
        .grand-total .total-value {{ color: white; font-size: 22px; font-weight: 700; }}
        
        .footer {{
            background: #FAFAFA;
            padding: 30px 40px;
            text-align: center;
            border-top: 1px solid #E5E5E5;
        }}
        .footer-message {{ font-size: 16px; color: #004732; font-weight: 500; margin-bottom: 10px; }}
        .footer-brand {{ font-size: 12px; color: #666666; letter-spacing: 1px; }}
        .footer-brand strong {{ color: #004732; }}
        
        /* Status Badge - Square style */
        .status-badge {{
            display: inline-flex;
            align-items: center;
            gap: 6px;
            padding: 6px 14px;
            border-radius: 20px;
            font-size: 12px;
            font-weight: 600;
            text-transform: uppercase;
            letter-spacing: 0.5px;
        }}
        .status-badge::before {{
            content: '';
            width: 8px;
            height: 8px;
            border-radius: 50%;
        }}
        .status-approved {{ background: #E8F5E9; color: #2E7D32; }}
        .status-approved::before {{ background: #2E7D32; }}
        .status-sent {{ background: #FFF3E0; color: #E65100; }}
        .status-sent::before {{ background: #E65100; }}
        .status-draft {{ background: #F5F5F5; color: #616161; }}
        .status-draft::before {{ background: #616161; }}
        
        /* Approve Section */
        .approve-section {{ margin-top: 10px; margin-bottom: 20px; }}
        .approve-card {{
            text-align: center;
            padding: 30px;
            background: #FAFAFA;
            border: 2px dashed #E0E0E0;
            border-radius: 16px;
        }}
        .approve-icon {{ font-size: 40px; margin-bottom: 12px; color: #004732; }}
        .approve-title {{ font-size: 18px; font-weight: 600; color: #1A1A1A; margin-bottom: 8px; }}
        .approve-subtitle {{ font-size: 13px; color: #666666; margin-bottom: 20px; max-width: 400px; margin-left: auto; margin-right: auto; }}
        .approve-button {{
            display: inline-block;
            background: #004732;
            color: white;
            border: none;
            padding: 14px 40px;
            border-radius: 12px;
            font-size: 16px;
            font-weight: 600;
            cursor: pointer;
            transition: all 0.2s;
            letter-spacing: 0.5px;
        }}
        .approve-button:hover {{ background: #006644; transform: translateY(-1px); box-shadow: 0 4px 12px rgba(0,71,50,0.3); }}
        .approve-button:active {{ transform: translateY(0); }}
        .approve-button:disabled {{ background: #BDBDBD; cursor: not-allowed; transform: none; box-shadow: none; }}
        
        .approved-card {{
            text-align: center;
            padding: 25px;
            background: #E8F5E9;
            border-radius: 16px;
            border: 1px solid #C8E6C9;
        }}
        .approved-icon {{ font-size: 32px; color: #2E7D32; margin-bottom: 8px; }}
        .approved-message {{ font-size: 16px; font-weight: 600; color: #2E7D32; margin-bottom: 8px; }}
        .approved-next-steps {{ font-size: 14px; color: #666666; max-width: 400px; margin: 0 auto; line-height: 1.6; }}

        @media (max-width: 600px) {{
            .header {{ padding: 25px; }}
            .info-bar {{ padding: 20px; flex-direction: column; }}
            .info-item {{ text-align: left; }}
            .content {{ padding: 25px; }}
            .client-section {{ gap: 20px; }}
            .client-box {{ min-width: 100%; }}
            .totals-box {{ width: 100%; }}
        }}
    </style>
</head>
<body>
    <div class=""document"">
        <div class=""header"">
            <div class=""header-content"">
                <div class=""brand-name"">SILKROAD</div>
                <div class=""company-name"">{companyName}</div>
                <div style=""display: flex; align-items: center; gap: 12px; flex-wrap: wrap;"">
                    <div class=""estimate-badge"">Estimate</div>
                    <div class=""status-badge {statusBadgeClass}"">{statusBadgeText}</div>
                </div>
            </div>
        </div>
        
        <div class=""info-bar"">
            <div class=""info-item"">
                <div class=""info-label"">Estimate Number</div>
                <div class=""info-value"">{estimateNumber}</div>
            </div>
            <div class=""info-item"">
                <div class=""info-label"">Proposed Start Date</div>
                <div class=""info-value"">{FormatDate(proposedStartDate)}</div>
            </div>
            <div class=""info-item"">
                <div class=""info-label"">Valid Until</div>
                <div class=""info-value"">{FormatDate(expirationDate)}</div>
            </div>
            <div class=""info-item"">
                <div class=""info-label"">Total Amount</div>
                <div class=""info-value"">{FormatCurrency(total)}</div>
            </div>
        </div>
        
        <div class=""content"">
            {clientSectionHtml}
            {validityNoticeHtml}
            {descriptionHtml}
            
            <table class=""items-table"">
                <thead>
                    <tr>
                        <th style=""width: 50px;"">#</th>
                        <th>Description</th>
                        <th class=""center"" style=""width: 80px;"">Qty</th>
                        <th class=""right"" style=""width: 120px;"">Unit Price</th>
                        <th class=""right"" style=""width: 120px;"">Amount</th>
                    </tr>
                </thead>
                <tbody>
                    {lineItemsHtml}
                </tbody>
            </table>
            
            <div class=""totals-wrapper"">
                <div class=""totals-box"">
                    <div class=""total-row"">
                        <span class=""total-label"">Subtotal</span>
                        <span class=""total-value"">{FormatCurrency(total)}</span>
                    </div>
                    <div class=""total-row"">
                        <span class=""total-label"">Tax</span>
                        <span class=""total-value"">$0.00</span>
                    </div>
                    <div class=""total-row grand-total"">
                        <span class=""total-label"">Total Due</span>
                        <span class=""total-value"">{FormatCurrency(total)}</span>
                    </div>
                </div>
            </div>
        </div>
        
        <div class=""content"">
            {approveButtonHtml}
        </div>

        <div class=""footer"">
            <div class=""footer-message"">Thank you for your business!</div>
            <div class=""footer-brand"">Powered by <strong>SilkRoad</strong></div>
        </div>
    </div>

    <script>
        async function approveEstimate() {{
            const btn = document.getElementById('approveBtn');
            if (!btn) return;
            
            btn.disabled = true;
            btn.textContent = 'Approving...';
            
            try {{
                const resp = await fetch('/Estimates/public/{shareId}/approve', {{ method: 'POST' }});
                if (resp.ok) {{
                    // Reload page to show approved state
                    window.location.reload();
                }} else {{
                    const data = await resp.json();
                    alert(data.Message || 'Failed to approve. Please try again.');
                    btn.disabled = false;
                    btn.textContent = 'Approve Estimate';
                }}
            }} catch (err) {{
                alert('Network error. Please check your connection and try again.');
                btn.disabled = false;
                btn.textContent = 'Approve Estimate';
            }}
        }}
    </script>
</body>
</html>";
    }
}
