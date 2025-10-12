import os
import shutil

# Set the root directory
root_dir = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))

# Define the directories to create/recreate
test_repos_dir = os.path.join(root_dir, 'testrepos')
play_repo_dir = os.path.join(test_repos_dir, '.playrepo')

# Clear and recreate the 'testrepos' directory
if os.path.exists(test_repos_dir):
    shutil.rmtree(test_repos_dir)
os.makedirs(test_repos_dir)

# Create the '.playrepo' directory
os.makedirs(play_repo_dir)

# Create the placeholder file
placeholder_path = os.path.join(play_repo_dir, 'placeholder')
with open(placeholder_path, 'w') as f:
    f.write('This is a placeholder file.')

print('Directories and placeholder file created successfully.')