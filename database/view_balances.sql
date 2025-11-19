-- View all card balances with details
SELECT 
    card_number,
    card_holder_name,
    balance,
    currency,
    card_type,
    expiry_date,
    is_active,
    CASE 
        WHEN expiry_date < CURRENT_TIMESTAMP THEN 'EXPIRED'
        WHEN NOT is_active THEN 'BLOCKED'
        ELSE 'ACTIVE'
    END as status
FROM credit_cards
ORDER BY balance DESC;

-- Summary statistics
SELECT 
    COUNT(*) as total_cards,
    COUNT(CASE WHEN is_active THEN 1 END) as active_cards,
    COUNT(CASE WHEN expiry_date < CURRENT_TIMESTAMP THEN 1 END) as expired_cards,
    SUM(balance) as total_balance,
    AVG(balance) as average_balance,
    MAX(balance) as highest_balance,
    MIN(balance) as lowest_balance
FROM credit_cards;

-- Top 5 highest balances
SELECT 
    card_holder_name,
    RIGHT(card_number, 4) as last_4_digits,
    balance,
    card_type
FROM credit_cards
WHERE is_active = true
ORDER BY balance DESC
LIMIT 5;

-- Cards with low balance (under 100)
SELECT 
    card_holder_name,
    RIGHT(card_number, 4) as last_4_digits,
    balance,
    CASE 
        WHEN balance = 0 THEN 'EMPTY'
        WHEN balance < 50 THEN 'CRITICAL'
        ELSE 'LOW'
    END as warning_level
FROM credit_cards
WHERE balance < 100
ORDER BY balance ASC;
