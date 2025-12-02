-- Add new tables for audit events, fraud alerts, and transaction metrics
-- Run this SQL script in PostgreSQL to create the new tables

-- Create audit_events table
CREATE TABLE IF NOT EXISTS audit_events (
    id UUID PRIMARY KEY,
    event_id VARCHAR(100) NOT NULL,
    event_type VARCHAR(100) NOT NULL,
    event_source VARCHAR(500) NOT NULL,
    event_subject VARCHAR(500),
    event_data TEXT,
    event_time TIMESTAMP NOT NULL,
    recorded_at TIMESTAMP NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_audit_event_type ON audit_events(event_type);
CREATE INDEX IF NOT EXISTS idx_audit_recorded_at ON audit_events(recorded_at);

-- Create fraud_alerts table
CREATE TABLE IF NOT EXISTS fraud_alerts (
    id UUID PRIMARY KEY,
    transaction_id VARCHAR(100) NOT NULL,
    risk_score INTEGER NOT NULL,
    alerts TEXT NOT NULL,
    amount DECIMAL(18,2) NOT NULL,
    currency VARCHAR(3) NOT NULL,
    detected_at TIMESTAMP NOT NULL,
    status VARCHAR(50) NOT NULL DEFAULT 'Pending'
);

CREATE INDEX IF NOT EXISTS idx_fraud_transaction_id ON fraud_alerts(transaction_id);
CREATE INDEX IF NOT EXISTS idx_fraud_risk_score ON fraud_alerts(risk_score);
CREATE INDEX IF NOT EXISTS idx_fraud_detected_at ON fraud_alerts(detected_at);

-- Create transaction_metrics table
CREATE TABLE IF NOT EXISTS transaction_metrics (
    id UUID PRIMARY KEY,
    metric_date TIMESTAMP NOT NULL,
    hour INTEGER NOT NULL,
    day_of_week VARCHAR(20) NOT NULL,
    currency VARCHAR(3) NOT NULL,
    transaction_count INTEGER NOT NULL DEFAULT 0,
    total_volume DECIMAL(18,2) NOT NULL DEFAULT 0,
    average_amount DECIMAL(18,2) NOT NULL DEFAULT 0,
    success_count INTEGER NOT NULL DEFAULT 0,
    failure_count INTEGER NOT NULL DEFAULT 0,
    last_updated TIMESTAMP NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_metric_date_hour_currency 
    ON transaction_metrics(metric_date, hour, currency);

-- Add comments for documentation
COMMENT ON TABLE audit_events IS 'Stores all Event Grid events for compliance and audit trail';
COMMENT ON TABLE fraud_alerts IS 'Stores fraud detection alerts with risk scores';
COMMENT ON TABLE transaction_metrics IS 'Aggregated transaction metrics by hour and currency';
