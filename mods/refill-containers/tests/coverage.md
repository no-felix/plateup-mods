# Refill Containers: verified candidate coverage

Mod 0.1.3. Native player 1.5.3-854F, Steam build 26000811, Windows, 2026-10-19.

54 candidates: 19 supported item types, 35 rejected. Supported types cover 38 complete recipe variants and 304 scenarios. Each supported scenario also repeats the transfer using the returned carrier.

These results cover isolated ECS interactions in the native player. See [testing limits](../../../docs/TESTING.md) for the remaining playthrough, multiplayer and save/load checks.

| Item | ID | Portions | Recipe variants | Native cases | Result |
|---|---:|---:|---:|---:|---|
| Avocado | 370822297 | 1 | 0 | 10 | Rejected: Single portion |
| Avocado Half - Stone | 1554615237 | 1 | 0 | 10 | Rejected: Single portion |
| Bamboo Pot Cooked | 2019756794 | 3 | 1 | 8 | Passed |
| Beans - Cooked | 1286433124 | 4 | 1 | 8 | Passed |
| Big Cake Tin - Baked | 1547040416 | 6 | 0 | 9 | Rejected: Non-portionable recipe template |
| Big Cake Tin - Flavoured | -1354941517 | 6 | 3 | 24 | Passed |
| Birthday Cake | -1950713115 | 99 | 0 | 9 | Rejected: No single reusable remainder |
| Boiled Potato - Mashed | -1341614392 | 20 | 1 | 8 | Passed |
| Bolognese - Cooked | -1089548751 | 2 | 1 | 8 | Passed |
| Bread - Baked | -1867438686 | 9 | 0 | 10 | Rejected: Remainder is not clean provider-backed equipment |
| Broccoli - Cooked Pot | 98665743 | 5 | 1 | 8 | Passed |
| Brownie Tray - Baked | -425645565 | 6 | 1 | 8 | Passed |
| Cookie Tray - Baked | -502245988 | 4 | 3 | 24 | Passed |
| Corn - Raw | 529258958 | 1 | 0 | 10 | Rejected: Single portion |
| Cupcake Tray - Baked | -1790201643 | 4 | 1 | 8 | Passed |
| Doughnut  Tray - Proofed | 1390528274 | 12 | 1 | 8 | Passed |
| Fish - Spiny Raw | -2047884387 | 1 | 0 | 10 | Rejected: Single portion |
| Gravy - Turkey Cooked | 294281422 | 20 | 0 | 10 | Rejected: Remainder is not clean provider-backed equipment |
| Homemade Ice Cream - Chocolate Mixed | 1815024071 | 5 | 1 | 8 | Passed |
| Homemade Ice Cream - Strawberry Mixed | 1186100927 | 5 | 1 | 8 | Passed |
| Homemade Ice Cream - Vanilla Mixed | -1033480722 | 5 | 1 | 8 | Passed |
| Lasagne - Burned | -106588634 | 4 | 0 | 9 | Rejected: Non-portionable recipe template |
| Lasagne - Cooked | -34663320 | 4 | 1 | 8 | Passed |
| Mandarin Raw | 1291848678 | 4 | 0 | 9 | Rejected: No single reusable remainder |
| Mixing Bowl - Mixed With Flavouring | -705806008 | 1 | 0 | 9 | Rejected: Single portion |
| Mixing Bowl - Mixed With Milk | 1211203800 | 1 | 0 | 9 | Rejected: Single portion |
| Nut Mixture - Baked | -1945246136 | 2 | 0 | 10 | Rejected: Remainder is not clean provider-backed equipment |
| Oil | -1900989960 | 9999 | 0 | 9 | Rejected: No single reusable remainder |
| Pizza - Cooked | -1196800934 | 3 | 0 | 10 | Rejected: Copy source |
| Plate - Dirty with Bone | 348289471 | 1 | 0 | 10 | Rejected: Single portion |
| Pot - Dirty | 1026000491 | 1 | 0 | 9 | Rejected: Single portion |
| Pot With Burned Doughnut | -1628910037 | 1 | 0 | 9 | Rejected: Single portion |
| Pot With Cooked Doughnut | 395533429 | 1 | 0 | 10 | Rejected: Single portion |
| Pot With Oil | 1719428613 | 1 | 0 | 9 | Rejected: Non-portionable recipe template |
| Potato - Chopped Pot Cooked | 2010203194 | 1 | 0 | 9 | Rejected: Single portion |
| Pumpkin | -165143951 | 1 | 0 | 10 | Rejected: Single portion |
| Roast Leg - Cooked | -1801513942 | 6 | 0 | 10 | Rejected: Remainder is not clean provider-backed equipment |
| Roux - Complete | -1681933989 | 2 | 1 | 8 | Passed |
| Sauce - Mushroom Cooked | 65943925 | 10 | 1 | 8 | Passed |
| Sauce - Red Cooked | 1690253467 | 10 | 1 | 8 | Passed |
| Soup - Broccoli Cheese Cooked | 226578993 | 3 | 0 | 10 | Rejected: Remainder is not clean provider-backed equipment |
| Soup - Carrot Cooked | -1582466042 | 3 | 0 | 10 | Rejected: Remainder is not clean provider-backed equipment |
| Soup - Meat Cooked | -1284423669 | 3 | 0 | 10 | Rejected: Remainder is not clean provider-backed equipment |
| Soup - Pumpkin Cooked | 407468560 | 3 | 0 | 10 | Rejected: Remainder is not clean provider-backed equipment |
| Soup - Tomato Cooked | 1752228187 | 3 | 0 | 10 | Rejected: Remainder is not clean provider-backed equipment |
| Spaghetti - Cooked | -1514413821 | 1 | 0 | 9 | Rejected: Non-portionable recipe template |
| Spaghetti - Drained | -748726515 | 2 | 1 | 8 | Passed |
| Stir Fry - Cooked | 150639636 | 1 | 0 | 10 | Rejected: Single portion |
| Sundae Syrup - Chocolate | 2105393112 | 9999 | 0 | 10 | Rejected: Source replenishes itself |
| Sundae Syrup - Strawberry | -764053970 | 9999 | 0 | 10 | Rejected: Source replenishes itself |
| Table Block Rubbish | -1863985141 | 1 | 0 | 9 | Rejected: Single portion |
| Taco - Individual | 244927287 | 4 | 0 | 9 | Rejected: Non-portionable recipe template |
| Tacos On Tray | -319704939 | 4 | 16 | 128 | Passed |
| Turkey - Cooked | -1568853395 | 4 | 0 | 10 | Rejected: Remainder is not clean provider-backed equipment |
