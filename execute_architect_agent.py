import os
import yaml
import subprocess

def read_yaml_files(directory):
    yaml_data = {}
    for root, _, files in os.walk(directory):
        for file in files:
            if file.endswith('.yml'):
                file_path = os.path.join(root, file)
                with open(file_path, 'r') as f:
                    data = yaml.safe_load(f)
                    yaml_data[file_path] = data
    return yaml_data

def execute_architect_agent(yaml_data):
    for file_path, data in yaml_data.items():
        # Assuming the architect agent is a command-line tool named `architect-agent`
        # and it takes YAML data as input via stdin.
        try:
            result = subprocess.run(['architect-agent'], input=yaml.dump(data), text=True, capture_output=True, check=True)
            print(f"Processed {file_path}:")
            print(result.stdout)
        except subprocess.CalledProcessError as e:
            print(f"Failed to process {file_path}:")
            print(e.stderr)

if __name__ == '__main__':
    directory = '.opencode'
    yaml_data = read_yaml_files(directory)
    execute_architect_agent(yaml_data)
