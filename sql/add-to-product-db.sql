-- Add Claude Connector to ForIT SaaS Product Database
-- Run against: forit-saas-db on forit-saas-sql.database.windows.net

-- 1. Add to products table (for SaaS access control)
IF NOT EXISTS (SELECT 1 FROM products WHERE slug = 'claude-connector')
BEGIN
    INSERT INTO products (slug, name, description, status, is_active)
    VALUES (
        'claude-connector',
        'Claude Connector',
        'Power Platform connector for Anthropic Claude API',
        'active',
        1
    );
END

-- 2. Add to product_pages table (for website display)
IF NOT EXISTS (SELECT 1 FROM product_pages WHERE slug = 'claude-connector')
BEGIN
    INSERT INTO product_pages (
        slug,
        title,
        subtitle,
        description,
        features,
        price_display,
        cta_text,
        cta_type,
        published,
        sort_order
    )
    VALUES (
        'claude-connector',
        'Claude Connector',
        'AI-Powered Power Automate',
        'Integrate Anthropic Claude AI into your Power Platform workflows. Simple prompt-in, response-out interface with advanced options for extended thinking and temperature control.',
        '["Simple Ask Claude action - prompt in, response out","Claude Sonnet 4.5 and Opus 4.5 models","Extended thinking for complex reasoning","Temperature control for deterministic outputs","Token counting and batch processing"]',
        'Free',
        'Get Started',
        'portal',
        1,
        10
    );
END

-- Verify
SELECT 'products' as table_name, slug, name, status FROM products WHERE slug = 'claude-connector'
UNION ALL
SELECT 'product_pages', slug, title, CAST(published as varchar) FROM product_pages WHERE slug = 'claude-connector';
