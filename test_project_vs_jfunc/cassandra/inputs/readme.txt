'model_input_data_original.csv' is the original set of treatment lengths before splitting longer
segments so that they can all receive a rehab without busting the budget.

'model_input_data.csv' is the adjusted treatment length set AFTER splitting to accommodate rehab cost

IMPORTANT!
I manually converted the Bus Routes assigned from the ONF table to an actual bus routes estimate. I did this
because I did not want to re-do the Data-Join which would mean splitting long treatments lengths again, preparing
committed treatments etc. Instead, I just applied the following in Excel:

1. Copied the file_no_bus_routes column to "onf_public_transport". This holds values 1 to 6 mapping to ONF public transport
categories PT1, PT2 etc to PT6 (no public transport - assumed?)
2. I re-calculated the values in file_no_bus_routes to give a ranking of likely number of bus routes, using mapping:

PT1 = 10
PT2 = 7
PT3 = 5
PT4 = 4
PT5 = 2
PT6 = 0
