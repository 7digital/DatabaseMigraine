ALTER TABLE [dbo].[ForFkTestingFoo]  WITH NOCHECK
	ADD CONSTRAINT [ForFkTestingFoo_ForFkTestingBar_FK] FOREIGN KEY([ForFkTestingBarID])
		REFERENCES [dbo].[ForFkTestingBar] ([ID]) NOT FOR REPLICATION
GO
ALTER TABLE [dbo].[ForFkTestingFoo] CHECK CONSTRAINT [ForFkTestingFoo_ForFkTestingBar_FK]
GO
