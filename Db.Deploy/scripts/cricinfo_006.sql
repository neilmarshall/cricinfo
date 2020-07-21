-- Updte innings table to add field to show if is a declaration

ALTER TABLE innings ADD COLUMN declared boolean NOT NULL DEFAULT FALSE;
