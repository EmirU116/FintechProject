-- Check if credit_cards table exists and view its data
-- Run this in pgAdmin

-- Check if table exists
SELECT EXISTS (
   SELECT FROM information_schema.tables 
   WHERE table_schema = 'public'
   AND table_name = 'credit_cards'
);

-- View all cards
SELECT * FROM credit_cards;

-- View card count
SELECT COUNT(*) as card_count FROM credit_cards;

-- View processed transactions
SELECT * FROM processed_transactions ORDER BY processed_at DESC LIMIT 10;
