#!/usr/bin/env python3
"""
GitHub Knowledge Agent - Learns from your repositories
Analyzes patterns, best practices, and common implementations from actual code
"""
import subprocess
import json
import os
import threading
import time
from datetime import datetime
import requests
import re

class GitHubKnowledgeAgent:
    def __init__(self):
        self.repo_patterns = {}
        self.code_templates = {}
        self.common_solutions = {}
        self.best_practices = {}
        self.running = True
        
        # Start analysis threads
        threading.Thread(target=self.analyze_local_repos, daemon=True).start()
        threading.Thread(target=self.learn_from_commits, daemon=True).start()
        
    def find_local_repositories(self):
        """Find all Git repositories on the system"""
        repos = []
        search_paths = [
            "/Users/jamestunick/Documents/GitHub",
            "/Users/jamestunick/Desktop", 
            "/Users/jamestunick/xrai"
        ]
        
        for search_path in search_paths:
            if os.path.exists(search_path):
                try:
                    # Find .git directories
                    result = subprocess.run([
                        "find", search_path, "-name", ".git", "-type", "d"
                    ], capture_output=True, text=True)
                    
                    for git_dir in result.stdout.strip().split('\n'):
                        if git_dir:
                            repo_path = os.path.dirname(git_dir)
                            repos.append(repo_path)
                except:
                    continue
                    
        return repos
        
    def analyze_repository_patterns(self, repo_path):
        """Analyze patterns in a specific repository"""
        patterns = {
            "languages": {},
            "file_structures": [],
            "common_imports": [],
            "function_patterns": [],
            "class_patterns": [],
            "documentation_style": "",
            "testing_patterns": []
        }
        
        try:
            # Get language distribution
            result = subprocess.run([
                "find", repo_path, "-type", "f", "-name", "*.py", "-o", "-name", "*.cs", "-o", "-name", "*.js", "-o", "-name", "*.ts"
            ], capture_output=True, text=True)
            
            files = [f for f in result.stdout.strip().split('\n') if f]
            
            for file_path in files[:50]:  # Analyze first 50 files
                try:
                    with open(file_path, 'r', encoding='utf-8') as f:
                        content = f.read()
                        
                    # Extract patterns
                    self.extract_code_patterns(content, file_path, patterns)
                    
                except Exception as e:
                    continue
                    
        except Exception as e:
            print(f"Repository analysis error: {e}")
            
        return patterns
        
    def extract_code_patterns(self, content, file_path, patterns):
        """Extract patterns from code content"""
        extension = os.path.splitext(file_path)[1]
        
        # Language detection
        if extension not in patterns["languages"]:
            patterns["languages"][extension] = 0
        patterns["languages"][extension] += 1
        
        # Common imports/using statements
        if extension == ".py":
            imports = re.findall(r'^import\s+(\S+)|^from\s+(\S+)', content, re.MULTILINE)
            for imp in imports:
                module = imp[0] or imp[1]
                if module and module not in patterns["common_imports"]:
                    patterns["common_imports"].append(module)
                    
        elif extension == ".cs":
            usings = re.findall(r'^using\s+([^;]+);', content, re.MULTILINE)
            patterns["common_imports"].extend(usings)
            
        # Function/method patterns
        if extension == ".py":
            functions = re.findall(r'def\s+(\w+)\s*\([^)]*\):', content)
            patterns["function_patterns"].extend(functions)
            
        elif extension == ".cs":
            methods = re.findall(r'(public|private|protected)\s+\w+\s+(\w+)\s*\(', content)
            patterns["function_patterns"].extend([m[1] for m in methods])
            
        # Class patterns
        classes = re.findall(r'class\s+(\w+)', content)
        patterns["class_patterns"].extend(classes)
        
        # Documentation patterns
        if "///" in content or '"""' in content:
            patterns["documentation_style"] = "well_documented"
        elif "//" in content or "#" in content:
            patterns["documentation_style"] = "basic_comments"
        else:
            patterns["documentation_style"] = "minimal"
            
    def learn_best_practices_from_commits(self, repo_path):
        """Learn best practices from commit messages and changes"""
        try:
            # Get recent commits
            result = subprocess.run([
                "git", "-C", repo_path, "log", "--oneline", "-50", "--grep=fix", "--grep=improve", "--grep=refactor"
            ], capture_output=True, text=True)
            
            commits = result.stdout.strip().split('\n')
            
            best_practices = []
            for commit in commits:
                if commit:
                    # Extract practice from commit message
                    if "fix" in commit.lower():
                        best_practices.append(f"Common fix pattern: {commit}")
                    elif "improve" in commit.lower() or "optimize" in commit.lower():
                        best_practices.append(f"Optimization pattern: {commit}")
                    elif "refactor" in commit.lower():
                        best_practices.append(f"Refactoring pattern: {commit}")
                        
            return best_practices
            
        except Exception as e:
            return []
            
    def extract_common_solutions(self, repo_path):
        """Extract common solution patterns from codebase"""
        solutions = {}
        
        try:
            # Look for utility functions
            result = subprocess.run([
                "grep", "-r", "-n", "def\\|function\\|public.*static", repo_path
            ], capture_output=True, text=True)
            
            for line in result.stdout.split('\n')[:100]:  # First 100 matches
                if ':' in line:
                    file_path, code = line.split(':', 1)
                    
                    # Categorize solutions
                    if "util" in file_path.lower() or "helper" in file_path.lower():
                        if "utilities" not in solutions:
                            solutions["utilities"] = []
                        solutions["utilities"].append(code.strip())
                        
                    elif "manager" in file_path.lower():
                        if "managers" not in solutions:
                            solutions["managers"] = []
                        solutions["managers"].append(code.strip())
                        
        except Exception as e:
            pass
            
        return solutions
        
    def generate_code_template(self, template_type, repo_patterns):
        """Generate code templates based on repository patterns"""
        templates = {}
        
        if template_type == "unity_component":
            # Generate Unity component template based on patterns
            common_methods = ["Start", "Update", "Awake", "OnEnable", "OnDisable"]
            found_methods = [m for m in repo_patterns.get("function_patterns", []) if m in common_methods]
            
            template = f"""using UnityEngine;

public class {{ComponentName}} : MonoBehaviour
{{
"""
            for method in found_methods:
                if method == "Start":
                    template += """    void Start()
    {
        // Initialize component
    }
    
"""
                elif method == "Update":
                    template += """    void Update()
    {
        // Update logic
    }
    
"""
                elif method == "Awake":
                    template += """    void Awake()
    {
        // Early initialization
    }
    
"""
                    
            template += "}"
            templates["unity_component"] = template
            
        elif template_type == "python_service":
            # Generate Python service template
            common_imports = repo_patterns.get("common_imports", [])
            
            template = f"""#!/usr/bin/env python3
'''
{{ServiceName}} - Service description
'''
"""
            if "threading" in common_imports:
                template += "import threading\n"
            if "json" in common_imports:
                template += "import json\n"
            if "subprocess" in common_imports:
                template += "import subprocess\n"
                
            template += """
class {ServiceName}:
    def __init__(self):
        self.running = False
        
    def start(self):
        '''Start the service'''
        self.running = True
        
    def stop(self):
        '''Stop the service'''
        self.running = False
        
if __name__ == "__main__":
    service = {ServiceName}()
    service.start()
"""
            templates["python_service"] = template
            
        return templates
        
    def analyze_local_repos(self):
        """Continuously analyze local repositories"""
        while self.running:
            try:
                repos = self.find_local_repositories()
                print(f"🔍 Analyzing {len(repos)} repositories...")
                
                for repo_path in repos:
                    repo_name = os.path.basename(repo_path)
                    print(f"📊 Analyzing: {repo_name}")
                    
                    # Analyze patterns
                    patterns = self.analyze_repository_patterns(repo_path)
                    self.repo_patterns[repo_name] = patterns
                    
                    # Extract solutions
                    solutions = self.extract_common_solutions(repo_path)
                    self.common_solutions[repo_name] = solutions
                    
                    # Learn best practices
                    practices = self.learn_best_practices_from_commits(repo_path)
                    self.best_practices[repo_name] = practices
                    
                # Save knowledge
                self.save_knowledge()
                
                time.sleep(300)  # Re-analyze every 5 minutes
                
            except Exception as e:
                print(f"Repository analysis error: {e}")
                time.sleep(60)
                
    def learn_from_commits(self):
        """Learn from recent commit patterns"""
        while self.running:
            try:
                repos = self.find_local_repositories()
                
                for repo_path in repos:
                    # Get recent commits
                    result = subprocess.run([
                        "git", "-C", repo_path, "log", "--oneline", "-10"
                    ], capture_output=True, text=True)
                    
                    commits = result.stdout.strip().split('\n')
                    
                    # Analyze commit patterns
                    for commit in commits:
                        if commit:
                            self.analyze_commit_message(commit, repo_path)
                            
                time.sleep(180)  # Check commits every 3 minutes
                
            except Exception as e:
                print(f"Commit learning error: {e}")
                time.sleep(60)
                
    def analyze_commit_message(self, commit, repo_path):
        """Analyze individual commit message for patterns"""
        try:
            if "fix" in commit.lower():
                # This indicates a common problem area
                self.record_common_issue(commit, repo_path)
                
            elif "add" in commit.lower() or "implement" in commit.lower():
                # This indicates a common feature pattern
                self.record_feature_pattern(commit, repo_path)
                
            elif "refactor" in commit.lower() or "improve" in commit.lower():
                # This indicates optimization opportunities
                self.record_optimization_pattern(commit, repo_path)
                
        except Exception as e:
            pass
            
    def record_common_issue(self, commit, repo_path):
        """Record common issues for proactive prevention"""
        repo_name = os.path.basename(repo_path)
        
        if "common_issues" not in self.repo_patterns:
            self.repo_patterns["common_issues"] = {}
            
        if repo_name not in self.repo_patterns["common_issues"]:
            self.repo_patterns["common_issues"][repo_name] = []
            
        self.repo_patterns["common_issues"][repo_name].append(commit)
        
    def record_feature_pattern(self, commit, repo_path):
        """Record feature implementation patterns"""
        repo_name = os.path.basename(repo_path)
        
        if "feature_patterns" not in self.repo_patterns:
            self.repo_patterns["feature_patterns"] = {}
            
        if repo_name not in self.repo_patterns["feature_patterns"]:
            self.repo_patterns["feature_patterns"][repo_name] = []
            
        self.repo_patterns["feature_patterns"][repo_name].append(commit)
        
    def record_optimization_pattern(self, commit, repo_path):
        """Record optimization patterns"""
        repo_name = os.path.basename(repo_path)
        
        if "optimization_patterns" not in self.repo_patterns:
            self.repo_patterns["optimization_patterns"] = {}
            
        if repo_name not in self.repo_patterns["optimization_patterns"]:
            self.repo_patterns["optimization_patterns"][repo_name] = []
            
        self.repo_patterns["optimization_patterns"][repo_name].append(commit)
        
    def save_knowledge(self):
        """Save learned knowledge to files"""
        knowledge_file = "/Users/jamestunick/xrai/github_knowledge.json"
        
        knowledge = {
            "repo_patterns": self.repo_patterns,
            "common_solutions": self.common_solutions,
            "best_practices": self.best_practices,
            "last_updated": datetime.now().isoformat()
        }
        
        with open(knowledge_file, 'w') as f:
            json.dump(knowledge, f, indent=2)
            
    def get_recommendation(self, context):
        """Get recommendations based on learned knowledge"""
        recommendations = []
        
        # Based on common issues
        if "common_issues" in self.repo_patterns:
            for repo, issues in self.repo_patterns["common_issues"].items():
                if context.lower() in repo.lower():
                    recommendations.append(f"Common issue in {repo}: Watch out for patterns like {issues[:3]}")
                    
        # Based on feature patterns
        if "feature_patterns" in self.repo_patterns:
            for repo, patterns in self.repo_patterns["feature_patterns"].items():
                if context.lower() in repo.lower():
                    recommendations.append(f"Feature pattern in {repo}: Consider approaches like {patterns[:3]}")
                    
        return recommendations
        
    def suggest_template(self, task_type):
        """Suggest code template based on learned patterns"""
        # Find most relevant repository
        relevant_repo = None
        for repo_name in self.repo_patterns:
            if task_type.lower() in repo_name.lower():
                relevant_repo = repo_name
                break
                
        if relevant_repo:
            patterns = self.repo_patterns[relevant_repo]
            return self.generate_code_template(task_type, patterns)
            
        return None

if __name__ == "__main__":
    print("📚 Starting GitHub Knowledge Agent...")
    print("🔍 Learning from your repositories...")
    
    agent = GitHubKnowledgeAgent()
    
    try:
        while True:
            # Show what we've learned
            print(f"\n📊 Knowledge Status:")
            print(f"  Repositories analyzed: {len(agent.repo_patterns)}")
            print(f"  Solutions cataloged: {len(agent.common_solutions)}")
            print(f"  Best practices found: {len(agent.best_practices)}")
            
            time.sleep(60)  # Status update every minute
            
    except KeyboardInterrupt:
        agent.running = False
        print("\n🛑 GitHub Knowledge Agent stopped")