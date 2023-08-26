# AIOrchestrator
### A sub project of [AIStoryBuilders](https://github.com/ADefWebserver/AIStoryBuilders)
![image](https://github.com/ADefWebserver/AIOrchestrator/assets/1857799/d8bc287f-5493-44e0-bdb6-6019636b23cf)

### Methods

1. **Master Instructions**
    - How many iterations it can have
    - Indicate what functions are avaliable
    - Indicate when a function should be called
    - Indicate when a function should not be called
2. **Initial Start**
    - Builds a list of tasks in the Database from Initial Prompt
        - (Ask the LLM to create a list of steps)
3. **Looping Through Task List**
    - If a list of tasks in the Database exists, it always loops through that list
4. **On Each Iteration**
    - Performs the Task and marks it complete
        - Calls a Function if needed to complete a Task 
          - Searches Memory if needed to complete a Task
          - Writes to Memory if needed to complete a Task
    - Updates the Log
    - Creates new Tasks, or inserts new Sub Tasks (Tasks inserted before the next expected Task) 
        - (Ask the LLM to examine the existing Task List in the Database and create new Sub Tasks if needed)
        - (A list can only have 100 Tasks total before refusing to add any more tasks)
    - Updates the Log
    - Looks at the master instructions to see if it needs to break out of the loop
5. **Final Response**
    - Uses the "Memory" and the Initial Prompt to produce the final response

