-- Create credit_cards table
CREATE TABLE IF NOT EXISTS credit_cards (
    id SERIAL PRIMARY KEY,
    card_number VARCHAR(20) NOT NULL,
    card_holder_name VARCHAR(100) NOT NULL,
    balance DECIMAL(18,2) NOT NULL,
    card_type VARCHAR(50) NOT NULL,
    expiry_date TIMESTAMP NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    CONSTRAINT idx_card_number UNIQUE (card_number)
);

-- Create index on card_number for faster lookups
CREATE INDEX IF NOT EXISTS idx_card_number ON credit_cards(card_number);

-- Insert dummy test cards data
INSERT INTO credit_cards (card_number, card_holder_name, balance, card_type, expiry_date, is_active)
VALUES 
    ('4111111111111111', 'John Doe', 5000.00, 'Visa', NOW() + INTERVAL '2 years', TRUE),
    ('5555555555554444', 'Jane Smith', 3500.00, 'Mastercard', NOW() + INTERVAL '3 years', TRUE),
    ('378282246310005', 'Bob Johnson', 10000.00, 'American Express', NOW() + INTERVAL '1 year', TRUE),
    ('4000000000000002', 'Alice Brown', 250.00, 'Visa', NOW() + INTERVAL '2 years', TRUE),
    ('5105105105105100', 'Charlie Wilson', 750.00, 'Mastercard', NOW() + INTERVAL '1 year', TRUE),
    ('4000000000000010', 'David Lee', 25.00, 'Visa', NOW() + INTERVAL '2 years', TRUE),
    ('4000000000000051', 'Emma Davis', 10.50, 'Visa', NOW() + INTERVAL '1 year', TRUE),
    ('4000000000000069', 'Grace Taylor', 500.00, 'Visa', NOW() - INTERVAL '6 months', TRUE),
    ('4242424242424242', 'Frank Miller', 1000.00, 'Visa', NOW() + INTERVAL '2 years', FALSE)
ON CONFLICT (card_number) DO NOTHING;

-- Grant necessary permissions (adjust username as needed)
-- GRANT ALL PRIVILEGES ON TABLE credit_cards TO your_user;
-- GRANT USAGE, SELECT ON SEQUENCE credit_cards_id_seq TO your_user;
