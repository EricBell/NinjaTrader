import click
import csv

@click.command()
@click.option('--file', required=True)
def run(file):
    print(f'file {file}')
    data_set = {}
    with open (file) as csv_file:
        csv_reader = csv.DictReader(csv_file)
        line_count = 0
        for row in csv_reader:
            if line_count == 0:
                print(f'column names are {", ".join(row)}')
            else:
                # print(f'\tAction {row["Action"]} Type {row["Type"]} Time {row["Time"]}')
                data_set[line_count] = row
                # if line_count % 9 == 0:
                #     print(f'\n')
            line_count += 1
        # print(sum([len(data_set[x]) for x in data_set ]))
        key_list = data_set.keys()
        set_count = key_list.count()
        print(f'count {set_count}') 
        # for key in data_set.keys():
        #     element = data_set[key]
        #     print(f'{key}, {element["Action"]}, {element["Time"]}')
        
              
if __name__ == "__main__":
    run()

    # Instrument,Action,Type,Quantity,Limit,Stop,State,Filled,Avg. price,Remaining,Name,Strategy,OCO,Time,Cancel,