-- PostgreSQL Database Setup Script
-- Run this script to create the database and table for local development

-- Create the database (connect to postgres database first)
-- CREATE DATABASE fintech_db;

-- Connect to fintech_db and run the following:

-- Create the processed_transactions table
CREATE TABLE IF NOT EXISTS processed_transactions (
    id SERIAL PRIMARY KEY,
    transaction_id VARCHAR(100) NOT NULL,
    card_number_masked VARCHAR(50) NOT NULL,
    amount DECIMAL(18,2) NOT NULL,
    currency VARCHAR(3) NOT NULL,
    transaction_timestamp TIMESTAMP NOT NULL,
    processed_at TIMESTAMP NOT NULL,
    authorization_status VARCHAR(50) NOT NULL,
    processing_message VARCHAR(500),
    CONSTRAINT chk_amount CHECK (amount >= 0)
);

-- Create indexes for better query performance
CREATE INDEX IF NOT EXISTS idx_transaction_id ON processed_transactions(transaction_id);
CREATE INDEX IF NOT EXISTS idx_processed_at ON processed_transactions(processed_at);

-- Verify the table was created
SELECT * FROM processed_transactions LIMIT 5;
