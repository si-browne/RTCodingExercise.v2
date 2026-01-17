INITIAL OVERALL DECOMPOSITION/THOUGHTS



•	Need to track state changes to plates (edit/updates)

o	specifically status, price, reservations, sales

•	Logs need creation but without heavy persistence, this will be (JSON or XML)

•	Not only persistence but also archival system and retention

•	Access to audit "bank" also a consideration



ACCEPTANCE DECOMPOSITION



Initial thought "this looks like a glorified log with permissions"

Also, essentially a micro feature in 4 hours, decent challenge if to be done right



•	Observer state check on plate, conditional for in "ForSale" status

•	conditional for when admin changes it to "Reserved" (check letter casing?)

•	An audit entry gets created (I choose JSON, by way of files and folders?)

•	the schema must get added to the audit entry, with validation logic and failure scenarios (old status, new status, timestamp, and user ID)

o	Audit\_Entry\_ID (PK)

o	timestamp

o	user ID

o	old status

o	new status



•	Observer state check on plate price when it is described explicitly as 5k to 4.5k?

o	I don't like this, its too explicit, I will make the check on any price change

•	An audit entry gets created (I choose JSON, by way of files and folders?)

•	It should include old and new price, with validation logic and failure scenarios

o	Audit\_Entry\_ID (PK)

o	timestamp

o	user ID

o	old price

o	new price

•	which values should be nullable? consider



•	implement an audit log button per plate (controller and view) which leads to a UI, that does:

•	if > 1 audit entries exist for a plate, list them in chronological order

•	output should show what changed and who made the change

o	what? this could be a nullable Enum type variable, I call them menus

•	this log should be exportable (more UI, service and controller)



•	finally, it needs to be fast and non thread blocking (TAP). If its written properly, it will be.

•	at this point I still havent looked at the code





Design



Investigating application architecture and going to create ERD for the Audit Log based on my initial decomposition.



Existing JSON in setup (+EF Core Migration):



{

&nbsp; "Id": "7C88B586-AABA-400A-8EF2-AF2073FC0CB2",

&nbsp; "Registration": "M66VEY",

&nbsp; "PurchasePrice": "469.26",

&nbsp; "SalePrice": "5995.00",

&nbsp; "Numbers": "66",

&nbsp; "Letters": "MCV"

}





Had to disable “just my code” when attaching process and getting debug to work locally, which I did get working so I could just see exactly was coming through the API:



Plates.json is a barebones migration implemented in EF Core. With a set schema. I need to add my ERD/Schema to this in order to have it fit with the app architecture.



I decided to not employ inheritance using existing namespace Catalog.Domain.Plate;



I want a separation of concerns for my Audit feature. I have identified 3 core entities conscious of keeping data efficient rather than bloated:





Indexing is fundamental to speed when querying data. Intending to add indexing to new EF entities.



Implementation



I wrote the ERD into modelbuilder and ran migration commands, \& update. This took a while and I had to add TrustServerCertificate=True to appsettings.json in the API.



Added Audit domain model classes next in the API project.



Next Added an interceptor (which I had to research), to intercept plate values, check the state and if a change is detected create an auditworkitem. This also hooked into mass transit using a consumer – which I verified was working later.



I wrote the audit controller next for export and view.



The core development of the feature forced me to consider how I can dynamically check and record a state change as an audit. This was the biggest challenge.



Frontend development implemented an audit log button to check each plate – and render the new view as per controller request. I added filtering to enable a report to be generated and also then tested the application – which felt like it was not slow.



Finally added tests for structural checks.





