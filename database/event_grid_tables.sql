-- Create audit_events table for compliance and regulatory audit trail
CREATE TABLE IF NOT EXISTS audit_events (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    event_id VARCHAR(255) NOT NULL,
    event_type VARCHAR(100) NOT NULL,
    event_source VARCHAR(255) NOT NULL,
    event_subject VARCHAR(255),
    event_data JSONB NOT NULL,
    event_time TIMESTAMP NOT NULL,
    recorded_at TIMESTAMP NOT NULL DEFAULT NOW(),
    CONSTRAINT unique_event_id UNIQUE (event_id)
);

CREATE INDEX idx_audit_events_type ON audit_events(event_type);
CREATE INDEX idx_audit_events_time ON audit_events(event_time DESC);
CREATE INDEX idx_audit_events_subject ON audit_events(event_subject);

COMMENT ON TABLE audit_events IS 'Immutable audit log of all Event Grid events for compliance';
COMMENT ON COLUMN audit_events.event_id IS 'CloudEvent ID from Event Grid';
COMMENT ON COLUMN audit_events.event_data IS 'Full event payload as JSON';

-- Create notification_logs table to track notification delivery
CREATE TABLE IF NOT EXISTS notification_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    transaction_id VARCHAR(255) NOT NULL,
    notification_type VARCHAR(50) NOT NULL, -- 'email', 'sms', 'push'
    recipient VARCHAR(255) NOT NULL,
    message TEXT NOT NULL,
    status VARCHAR(50) NOT NULL, -- 'pending', 'sent', 'failed'
    sent_at TIMESTAMP,
    failed_reason TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_notification_logs_transaction ON notification_logs(transaction_id);
CREATE INDEX idx_notification_logs_status ON notification_logs(status);
CREATE INDEX idx_notification_logs_created ON notification_logs(created_at DESC);

COMMENT ON TABLE notification_logs IS 'Tracks notification delivery status for transaction events';

-- Create fraud_alerts table for fraud detection results
CREATE TABLE IF NOT EXISTS fraud_alerts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    transaction_id VARCHAR(255) NOT NULL,
    risk_score INTEGER NOT NULL,
    alert_type VARCHAR(100) NOT NULL, -- 'large_amount', 'velocity', 'unusual_pattern'
    alert_reason TEXT NOT NULL,
    from_card_masked VARCHAR(50),
    to_card_masked VARCHAR(50),
    amount DECIMAL(18, 2),
    currency VARCHAR(10),
    status VARCHAR(50) NOT NULL DEFAULT 'pending', -- 'pending', 'reviewed', 'false_positive', 'confirmed_fraud'
    reviewed_by VARCHAR(255),
    reviewed_at TIMESTAMP,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_fraud_alerts_transaction ON fraud_alerts(transaction_id);
CREATE INDEX idx_fraud_alerts_status ON fraud_alerts(status);
CREATE INDEX idx_fraud_alerts_risk_score ON fraud_alerts(risk_score DESC);
CREATE INDEX idx_fraud_alerts_created ON fraud_alerts(created_at DESC);

COMMENT ON TABLE fraud_alerts IS 'Stores fraud detection alerts for review';
COMMENT ON COLUMN fraud_alerts.risk_score IS 'Calculated risk score (0-100)';

-- Create transaction_metrics table for analytics aggregation
CREATE TABLE IF NOT EXISTS transaction_metrics (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    metric_date DATE NOT NULL,
    metric_hour INTEGER NOT NULL CHECK (metric_hour >= 0 AND metric_hour < 24),
    currency VARCHAR(10) NOT NULL,
    transaction_count INTEGER NOT NULL DEFAULT 0,
    success_count INTEGER NOT NULL DEFAULT 0,
    failed_count INTEGER NOT NULL DEFAULT 0,
    total_volume DECIMAL(18, 2) NOT NULL DEFAULT 0,
    avg_transaction_amount DECIMAL(18, 2),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    CONSTRAINT unique_metric_hour UNIQUE (metric_date, metric_hour, currency)
);

CREATE INDEX idx_transaction_metrics_date ON transaction_metrics(metric_date DESC);
CREATE INDEX idx_transaction_metrics_currency ON transaction_metrics(currency);

COMMENT ON TABLE transaction_metrics IS 'Aggregated transaction metrics for analytics and reporting';

-- Insert sample query views for easy monitoring
CREATE OR REPLACE VIEW recent_audit_events AS
SELECT 
    event_type,
    event_subject,
    event_time,
    recorded_at,
    event_data->>'transactionId' as transaction_id
FROM audit_events
WHERE recorded_at > NOW() - INTERVAL '24 hours'
ORDER BY recorded_at DESC;

CREATE OR REPLACE VIEW pending_fraud_alerts AS
SELECT 
    id,
    transaction_id,
    risk_score,
    alert_type,
    alert_reason,
    amount,
    currency,
    created_at
FROM fraud_alerts
WHERE status = 'pending'
ORDER BY risk_score DESC, created_at DESC;

CREATE OR REPLACE VIEW daily_transaction_summary AS
SELECT 
    metric_date,
    currency,
    SUM(transaction_count) as total_transactions,
    SUM(success_count) as successful_transactions,
    SUM(failed_count) as failed_transactions,
    SUM(total_volume) as total_volume,
    ROUND(AVG(avg_transaction_amount), 2) as avg_amount
FROM transaction_metrics
WHERE metric_date > CURRENT_DATE - INTERVAL '30 days'
GROUP BY metric_date, currency
ORDER BY metric_date DESC, currency;

COMMENT ON VIEW recent_audit_events IS 'Last 24 hours of audit events for quick monitoring';
COMMENT ON VIEW pending_fraud_alerts IS 'Fraud alerts awaiting review, sorted by risk score';
COMMENT ON VIEW daily_transaction_summary IS 'Daily transaction metrics for the last 30 days';
