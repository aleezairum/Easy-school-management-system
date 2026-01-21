-- Seed Payment Methods
-- Run this script to insert default payment methods

INSERT INTO PaymentMethods (Code, Name, Description, BankName, AccountNumber, BranchCode, IsActive, CreatedAt)
VALUES
    ('JAZZ', 'JazzCash', 'Mobile wallet payment via JazzCash', 'JazzCash', NULL, NULL, 1, GETUTCDATE()),
    ('EASY', 'EasyPaisa', 'Mobile wallet payment via EasyPaisa', 'EasyPaisa', NULL, NULL, 1, GETUTCDATE()),
    ('CASH', 'Cash', 'Cash payment at school office', NULL, NULL, NULL, 1, GETUTCDATE()),
    ('BANK', 'Bank Transfer', 'Direct bank transfer/deposit', NULL, NULL, NULL, 1, GETUTCDATE());

-- Verify insertion
SELECT * FROM PaymentMethods;
