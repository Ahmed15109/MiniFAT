

MiniFAT – Command Guide 

1) Create a file
Command:    touch a.txt


2) Write to file
Command:     write a.txt "hello"



3) Read file content
Command:       type a.txt



4) Show file metadata
Command:     stat a.txt
Descrip: Shows size, first cluster, and type.



5) Show FAT chain 
Command:    fat a.txt
Description: Displays cluster chain.



6) Write large content (multi cluster test)
Command: write a.txt "<lablablablablabla>"



7) Verify multi cluster allocation
Commands:    
→  stat a.txt 
→  fat a.txt



8) Create directory
Command:  mkdir d1



9) Enter directory
Command:    cd d1



10) Create file inside directory
Commands:  
	1. touch b.txt 
	2.  write b.txt "x" 
	3.  type b.txt



11) Return to root
Command:   cd /



12) List directory
Command:  ls



13) Copy file
Command:   copy a.txt c.txt



14) Rename file
Command:     ren c.txt z.txt



15) Delete file
Command:    del a.txt



16) Remove directory
Commands: 
cd d1
del b.txt
cd /
rd d1



17) Import file from host
Command: import "E:\os_test\os.txt" os.txt

   →  ls
   →  stat os.txt
   →  type os.txt


18) Export file to host
Command: export os.txt "E:\os_test\out.txt"



19) Format disk (delete all data) 
	Command: format yes → ls ls→ stat a.txt
	   	

20) Exit program
Command: exit
