TRUNCATE `user_roles`;
TRUNCATE `role_types`;
TRUNCATE `project_members`;
TRUNCATE `time_entry`;
TRUNCATE `signon`;
TRUNCATE `categories`;
TRUNCATE `project`;
TRUNCATE `users`;

INSERT INTO role_types (RoleType, Description) VALUES 
('admin', 'Project administrator may additionally view the list of all users.'),
('consultant', 'Consultant may log time entries only.'),
('manager', 'Project manager may additionally edit all projects and view reports.');

INSERT INTO users (Username, Password, EmailAddress, Disabled) VALUES 
('admin', '21232f297a57a5a743894a0e4a801fc3', 'admin@pradosoft.com', 0),
('manager', '1d0258c2440a8d19e716292b231e3190', 'manager@pradosoft.com', 0),
('consultant', '7adfa4f2ba9323e6c1e024de375434b0', 'consultant@pradosoft.com', 0);

INSERT INTO user_roles (UserID, RoleType) VALUES 
('admin', 'admin'),
('admin', 'manager'),
('admin', 'consultant'),
('manager', 'manager'),
('manager', 'consultant'),
('consultant', 'consultant');