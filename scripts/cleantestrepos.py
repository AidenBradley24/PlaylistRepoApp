import os
import shutil

abspath = os.path.abspath(__file__) + "../"
dname = os.path.dirname(abspath)
os.chdir(dname)

# Safely delete 'testrepos' and print any errors
if os.path.exists("testrepos"):
    try:
        shutil.rmtree("testrepos")
    except Exception as e:
        print(f"Could not delete 'testrepos': {e}")

# Recreate directory and file
os.mkdir("testrepos")
with open("testrepos/placeholder", 'w') as f:
    f.write("placeholder")
