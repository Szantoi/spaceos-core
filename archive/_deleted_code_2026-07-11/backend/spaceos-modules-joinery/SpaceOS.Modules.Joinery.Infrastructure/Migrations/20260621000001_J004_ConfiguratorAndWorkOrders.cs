using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Modules.Joinery.Infrastructure.Migrations;

/// <inheritdoc />
public partial class J004_ConfiguratorAndWorkOrders : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ProductTemplates table - Configuration rules (tenant-independent)
        migrationBuilder.Sql(@"
CREATE TABLE spaceos_joinery.""ProductTemplates"" (
    ""Id""                  varchar(50)     NOT NULL,
    ""Name""                varchar(100)    NOT NULL,
    ""Category""            varchar(50)     NULL,
    ""DimensionRules""      jsonb           NOT NULL DEFAULT '{}'::jsonb,
    ""AllowedMaterials""    jsonb           NOT NULL DEFAULT '[]'::jsonb,
    ""AllowedFittings""     jsonb           NOT NULL DEFAULT '[]'::jsonb,
    ""PricingRules""        jsonb           NOT NULL DEFAULT '{}'::jsonb,
    ""LeadTimeDays""        integer         NOT NULL DEFAULT 7,
    ""CreatedAt""           timestamptz     NOT NULL DEFAULT now(),
    CONSTRAINT ""PK_ProductTemplates"" PRIMARY KEY (""Id"")
);");

        // ProductConfigurations table - Saved configurations (tenant-scoped)
        migrationBuilder.Sql(@"
CREATE TABLE spaceos_joinery.""ProductConfigurations"" (
    ""Id""                  uuid            NOT NULL,
    ""TenantId""            uuid            NOT NULL,
    ""ProductType""         varchar(50)     NOT NULL,
    ""Params""              jsonb           NOT NULL DEFAULT '{}'::jsonb,
    ""BomSnapshot""         jsonb           NOT NULL DEFAULT '[]'::jsonb,
    ""EstimatedPrice""      numeric(10,2)   NOT NULL,
    ""PreviewUrl""          text            NULL,
    ""CreatedAt""           timestamptz     NOT NULL DEFAULT now(),
    ""CreatedBy""           uuid            NULL,
    CONSTRAINT ""PK_ProductConfigurations"" PRIMARY KEY (""Id"")
);");

        // WorkOrders table - Production orders (tenant-scoped)
        migrationBuilder.Sql(@"
CREATE TABLE spaceos_joinery.""WorkOrders"" (
    ""Id""                      uuid            NOT NULL,
    ""TenantId""                uuid            NOT NULL,
    ""ConfigurationId""         uuid            NOT NULL,
    ""Quantity""                integer         NOT NULL,
    ""DeliveryDate""            date            NOT NULL,
    ""CustomerRef""             varchar(100)    NULL,
    ""Notes""                   text            NULL,
    ""BomItems""                jsonb           NOT NULL DEFAULT '[]'::jsonb,
    ""TotalMaterialCost""       numeric(12,2)   NOT NULL,
    ""EstimatedLabor""          numeric(12,2)   NOT NULL,
    ""TotalCost""               numeric(12,2)   NOT NULL,
    ""ScheduledStart""          date            NOT NULL,
    ""EstimatedCompletion""     date            NOT NULL,
    ""PdfUrl""                  text            NULL,
    ""CreatedAt""               timestamptz     NOT NULL DEFAULT now(),
    ""CreatedBy""               uuid            NULL,
    CONSTRAINT ""PK_WorkOrders"" PRIMARY KEY (""Id""),
    CONSTRAINT ""FK_WorkOrders_ProductConfigurations"" FOREIGN KEY (""ConfigurationId"")
        REFERENCES spaceos_joinery.""ProductConfigurations"" (""Id"") ON DELETE RESTRICT
);");

        // Indexes
        migrationBuilder.Sql(@"CREATE INDEX ""IX_ProductConfigurations_TenantId"" ON spaceos_joinery.""ProductConfigurations"" (""TenantId"");");
        migrationBuilder.Sql(@"CREATE INDEX ""IX_ProductConfigurations_ProductType"" ON spaceos_joinery.""ProductConfigurations"" (""ProductType"");");
        migrationBuilder.Sql(@"CREATE INDEX ""IX_ProductConfigurations_CreatedAt"" ON spaceos_joinery.""ProductConfigurations"" (""CreatedAt"" DESC);");
        migrationBuilder.Sql(@"CREATE INDEX ""IX_WorkOrders_TenantId"" ON spaceos_joinery.""WorkOrders"" (""TenantId"");");
        migrationBuilder.Sql(@"CREATE INDEX ""IX_WorkOrders_ConfigurationId"" ON spaceos_joinery.""WorkOrders"" (""ConfigurationId"");");
        migrationBuilder.Sql(@"CREATE INDEX ""IX_WorkOrders_DeliveryDate"" ON spaceos_joinery.""WorkOrders"" (""DeliveryDate"");");

        // RLS for ProductConfigurations
        migrationBuilder.Sql(@"ALTER TABLE spaceos_joinery.""ProductConfigurations"" ENABLE ROW LEVEL SECURITY;");
        migrationBuilder.Sql(@"ALTER TABLE spaceos_joinery.""ProductConfigurations"" FORCE ROW LEVEL SECURITY;");
        migrationBuilder.Sql(@"
CREATE POLICY tenant_isolation ON spaceos_joinery.""ProductConfigurations""
    USING (""TenantId"" = current_setting('app.tenant_id', true)::uuid);");

        // RLS for WorkOrders
        migrationBuilder.Sql(@"ALTER TABLE spaceos_joinery.""WorkOrders"" ENABLE ROW LEVEL SECURITY;");
        migrationBuilder.Sql(@"ALTER TABLE spaceos_joinery.""WorkOrders"" FORCE ROW LEVEL SECURITY;");
        migrationBuilder.Sql(@"
CREATE POLICY tenant_isolation ON spaceos_joinery.""WorkOrders""
    USING (""TenantId"" = current_setting('app.tenant_id', true)::uuid);");

        // Seed 5 door templates
        migrationBuilder.Sql(@"
INSERT INTO spaceos_joinery.""ProductTemplates"" (""Id"", ""Name"", ""Category"", ""DimensionRules"", ""AllowedMaterials"", ""AllowedFittings"", ""PricingRules"", ""LeadTimeDays"") VALUES
('standard_door', 'Standard beltéri ajtó', 'doors',
    '{""minWidth"": 700, ""maxWidth"": 1100, ""minHeight"": 1900, ""maxHeight"": 2200, ""allowedThickness"": [40, 45]}'::jsonb,
    '[
        {""id"": ""chipboard_18mm"", ""name"": ""Forgácslap 18mm"", ""type"": ""core"", ""unitPrice"": 8500},
        {""id"": ""mdf_16mm"", ""name"": ""MDF 16mm"", ""type"": ""core"", ""unitPrice"": 9200},
        {""id"": ""oak_veneer"", ""name"": ""Tölgy furnér"", ""type"": ""veneer"", ""unitPrice"": 5200},
        {""id"": ""walnut_veneer"", ""name"": ""Dió furnér"", ""type"": ""veneer"", ""unitPrice"": 6100},
        {""id"": ""pvc_edge_2mm"", ""name"": ""PVC élzáró 2mm"", ""type"": ""edge"", ""unitPrice"": 450}
    ]'::jsonb,
    '[
        {""id"": ""hidden_3d"", ""name"": ""Rejtett 3D zsanér"", ""category"": ""hinge"", ""unitPrice"": 1200},
        {""id"": ""standard_hinge"", ""name"": ""Standard zsanér"", ""category"": ""hinge"", ""unitPrice"": 650},
        {""id"": ""modern_steel"", ""name"": ""Modern acél kilincs"", ""category"": ""handle"", ""unitPrice"": 4500},
        {""id"": ""classic_brass"", ""name"": ""Klasszikus réz kilincs"", ""category"": ""handle"", ""unitPrice"": 5200},
        {""id"": ""standard_cylinder"", ""name"": ""Standard henger zár"", ""category"": ""lock"", ""unitPrice"": 3200}
    ]'::jsonb,
    '{""laborRate"": 5000, ""marginPercent"": 15, ""setupCost"": 2000}'::jsonb,
    7),

('premium_door', 'Prémium beltéri ajtó', 'doors',
    '{""minWidth"": 700, ""maxWidth"": 1200, ""minHeight"": 1900, ""maxHeight"": 2400, ""allowedThickness"": [40, 45, 50]}'::jsonb,
    '[
        {""id"": ""solid_core_mdf"", ""name"": ""Tömör MDF mag"", ""type"": ""core"", ""unitPrice"": 14500},
        {""id"": ""honeycomb_core"", ""name"": ""Méhsejt mag"", ""type"": ""core"", ""unitPrice"": 11200},
        {""id"": ""oak_veneer_premium"", ""name"": ""Prémium tölgy furnér"", ""type"": ""veneer"", ""unitPrice"": 8900},
        {""id"": ""ash_veneer"", ""name"": ""Kőris furnér"", ""type"": ""veneer"", ""unitPrice"": 7600},
        {""id"": ""abs_edge_1mm"", ""name"": ""ABS élzáró 1mm"", ""type"": ""edge"", ""unitPrice"": 680}
    ]'::jsonb,
    '[
        {""id"": ""concealed_adjustable"", ""name"": ""Rejtett állítható zsanér"", ""category"": ""hinge"", ""unitPrice"": 2400},
        {""id"": ""designer_handle"", ""name"": ""Dizájner kilincs"", ""category"": ""handle"", ""unitPrice"": 8900},
        {""id"": ""magnetic_lock"", ""name"": ""Mágneszár"", ""category"": ""lock"", ""unitPrice"": 5600}
    ]'::jsonb,
    '{""laborRate"": 7500, ""marginPercent"": 20, ""setupCost"": 3500}'::jsonb,
    10),

('fireproof_door', 'Tűzálló ajtó EI30', 'doors',
    '{""minWidth"": 800, ""maxWidth"": 1000, ""minHeight"": 2000, ""maxHeight"": 2100, ""allowedThickness"": [45, 50]}'::jsonb,
    '[
        {""id"": ""fire_rated_core"", ""name"": ""Tűzálló mag EI30"", ""type"": ""core"", ""unitPrice"": 24000},
        {""id"": ""fire_rated_core_ei60"", ""name"": ""Tűzálló mag EI60"", ""type"": ""core"", ""unitPrice"": 32000},
        {""id"": ""laminate_fire"", ""name"": ""Tűzálló laminált felület"", ""type"": ""veneer"", ""unitPrice"": 4200},
        {""id"": ""intumescent_strip"", ""name"": ""Duzzadó tömítés"", ""type"": ""seal"", ""unitPrice"": 2800}
    ]'::jsonb,
    '[
        {""id"": ""fire_rated_hinge"", ""name"": ""Tűzálló zsanér"", ""category"": ""hinge"", ""unitPrice"": 3200},
        {""id"": ""fire_door_closer"", ""name"": ""Ajtócsukó tűzgátló"", ""category"": ""closer"", ""unitPrice"": 12500},
        {""id"": ""panic_bar"", ""name"": ""Pánikrúd"", ""category"": ""handle"", ""unitPrice"": 18000}
    ]'::jsonb,
    '{""laborRate"": 12000, ""marginPercent"": 25, ""setupCost"": 5000}'::jsonb,
    14),

('acoustic_door', 'Hangszigetelt ajtó Rw 37dB', 'doors',
    '{""minWidth"": 800, ""maxWidth"": 1000, ""minHeight"": 2000, ""maxHeight"": 2200, ""allowedThickness"": [45, 50, 55]}'::jsonb,
    '[
        {""id"": ""acoustic_core_37"", ""name"": ""Hangszigetelt mag 37dB"", ""type"": ""core"", ""unitPrice"": 28000},
        {""id"": ""acoustic_core_42"", ""name"": ""Hangszigetelt mag 42dB"", ""type"": ""core"", ""unitPrice"": 36000},
        {""id"": ""acoustic_seal"", ""name"": ""Akusztikus tömítés"", ""type"": ""seal"", ""unitPrice"": 3500},
        {""id"": ""drop_seal"", ""name"": ""Süllyesztett küszöbtömítés"", ""type"": ""seal"", ""unitPrice"": 8500}
    ]'::jsonb,
    '[
        {""id"": ""acoustic_hinge"", ""name"": ""Hangszigetelt zsanér"", ""category"": ""hinge"", ""unitPrice"": 2800},
        {""id"": ""acoustic_handle"", ""name"": ""Hangszigetelt kilincs"", ""category"": ""handle"", ""unitPrice"": 6200},
        {""id"": ""acoustic_lock"", ""name"": ""Akusztikus zár"", ""category"": ""lock"", ""unitPrice"": 4800}
    ]'::jsonb,
    '{""laborRate"": 10000, ""marginPercent"": 22, ""setupCost"": 4500}'::jsonb,
    12),

('security_door', 'Biztonsági ajtó RC3', 'doors',
    '{""minWidth"": 850, ""maxWidth"": 1050, ""minHeight"": 2000, ""maxHeight"": 2150, ""allowedThickness"": [50, 55, 60]}'::jsonb,
    '[
        {""id"": ""steel_reinforced_core"", ""name"": ""Acélmegerősített mag"", ""type"": ""core"", ""unitPrice"": 45000},
        {""id"": ""anti_drill_plate"", ""name"": ""Fúrásvédő lemez"", ""type"": ""core"", ""unitPrice"": 12000},
        {""id"": ""security_laminate"", ""name"": ""Biztonsági laminát"", ""type"": ""veneer"", ""unitPrice"": 8500},
        {""id"": ""steel_frame"", ""name"": ""Acél tok"", ""type"": ""frame"", ""unitPrice"": 35000}
    ]'::jsonb,
    '[
        {""id"": ""security_hinge"", ""name"": ""Biztonsági zsanér"", ""category"": ""hinge"", ""unitPrice"": 4500},
        {""id"": ""multipoint_lock"", ""name"": ""Többpontos zár"", ""category"": ""lock"", ""unitPrice"": 28000},
        {""id"": ""security_handle"", ""name"": ""Biztonsági kilincs"", ""category"": ""handle"", ""unitPrice"": 9500},
        {""id"": ""door_chain"", ""name"": ""Biztonsági lánc"", ""category"": ""accessory"", ""unitPrice"": 3200}
    ]'::jsonb,
    '{""laborRate"": 15000, ""marginPercent"": 30, ""setupCost"": 8000}'::jsonb,
    21)
ON CONFLICT DO NOTHING;");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"DROP TABLE IF EXISTS spaceos_joinery.""WorkOrders"";");
        migrationBuilder.Sql(@"DROP TABLE IF EXISTS spaceos_joinery.""ProductConfigurations"";");
        migrationBuilder.Sql(@"DROP TABLE IF EXISTS spaceos_joinery.""ProductTemplates"";");
    }
}
