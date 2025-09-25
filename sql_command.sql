/** follower **/

Select * from Followers

Update Followers
SET Role = 1
Where UserId In ('2117908013271116630', '3455936902954109127', '2592377314198964427');

Update Followers
SET RoleDetail = 'manager'
Where UserId = '3455936902954109127'


/** data fake **/

INSERT INTO SVN_daily_target_test (
    Operation, Daily_plan, UPH, UPPH, Labor, Defect, Date_time, Workingtime, Total_Qty, MaxLabor, WC, Current_UPH, Current_UPPH, Total_NG_Qty
) VALUES
('POP', 687.00, 313.55, 26.13, 12, 0.01, '20250919', 2.19, 734.00, 12, 'FG', 335.16, 27.93, 0.00),
('Injection_PQP', 2215.00, 1020.65, 510.32, 2, 0.01, '20250919', 2.17, 2292.00, 2, 'FG', 1056.22, 528.11, 0.00),
('Walter-WSS03-00347', 456.00, 200.00, 13.33, 15, 0.01, '20250919', 2.28, 450.00, 15, 'FG', 197.37, 13.16, 0.00);

SELECT * FROM SVN_daily_target_test

UPDATE SVN_daily_target_test
SET Daily_plan = 10000, UPH = 10000, Total_NG_Qty=400
WHERE Date_time = '20250919'


/** message **/

select * from dbo.SVN_Messages

where create_at = '2025-09-18 17:24:01.753'

DELETE FROM dbo.SVN_Messages

DBCC CHECKIDENT('dbo.SVN_Messages', RESEED, 0);

UPDATE dbo.SVN_Messages
SET isSentToManager = 1
WHERE id_message In ('13', '14', '15');