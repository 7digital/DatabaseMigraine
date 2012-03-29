CREATE TABLE LnkTesting (
	[ForFkTestingBarID] INT NOT NULL,
	[ForFkTestingFooID] INT NOT NULL,
	CONSTRAINT [LnkTesting_PK] PRIMARY KEY ( [ForFkTestingBarID],[ForFkTestingFooID] )
) ON [PRIMARY]
GO
