#!/usr/bin/env python3
"""
Elite Repositories Knowledge Agent
Learns from high-quality GitHub repos (1000+ stars, active contributors, 12+ months)
Extracts battle-tested patterns and best practices
"""
import subprocess
import json
import os
import threading
import time
from datetime import datetime, timedelta
import requests
import re

class EliteReposAgent:
    def __init__(self):
        self.elite_repos = {}
        self.knowledge_base = {}
        self.patterns = {}
        self.best_practices = {}
        self.running = True
        
        # GitHub API token (optional, for higher rate limits)
        self.github_token = os.environ.get('GITHUB_TOKEN', '')
        
        # Start learning threads
        threading.Thread(target=self.discover_elite_repos, daemon=True).start()
        threading.Thread(target=self.analyze_elite_patterns, daemon=True).start()
        
    def get_github_headers(self):
        """Get headers for GitHub API requests"""
        headers = {
            'Accept': 'application/vnd.github.v3+json',
            'User-Agent': 'XRAI-Knowledge-Agent'
        }
        if self.github_token:
            headers['Authorization'] = f'token {self.github_token}'
        return headers
        
    def discover_elite_repos(self):
        """Discover elite repositories based on criteria"""
        while self.running:
            try:
                categories = [
                    # AI/ML repositories
                    'machine-learning+language:python+stars:>1000',
                    'artificial-intelligence+language:python+stars:>1000',
                    'deep-learning+language:python+stars:>1000',
                    
                    # Unity/Game Development
                    'unity+language:csharp+stars:>1000',
                    'game-development+language:csharp+stars:>1000',
                    'vr+language:csharp+stars:>1000',
                    
                    # Voice/Audio
                    'speech-recognition+stars:>1000',
                    'text-to-speech+stars:>1000',
                    'audio-processing+stars:>1000',
                    
                    # System/Tools
                    'automation+language:python+stars:>1000',
                    'developer-tools+stars:>1000',
                    'productivity+stars:>1000',
                    
                    # Web/Visualization
                    'data-visualization+language:javascript+stars:>1000',
                    'charts+language:javascript+stars:>1000',
                    'dashboard+language:javascript+stars:>1000'
                ]
                
                for category in categories:
                    print(f"🔍 Discovering elite repos in: {category}")
                    elite_repos = self.search_elite_repos(category)
                    
                    for repo in elite_repos:
                        self.analyze_repo_quality(repo)
                        time.sleep(1)  # Rate limiting
                        
                    time.sleep(10)  # Pause between categories
                    
                # Save discovered repos
                self.save_elite_repos()
                
                time.sleep(3600)  # Re-discover every hour
                
            except Exception as e:
                print(f"Discovery error: {e}")
                time.sleep(300)
                
    def search_elite_repos(self, query):
        """Search for elite repositories"""
        try:
            url = f"https://api.github.com/search/repositories"
            params = {
                'q': query,
                'sort': 'stars',
                'order': 'desc',
                'per_page': 20
            }
            
            response = requests.get(url, params=params, headers=self.get_github_headers())
            
            if response.status_code == 200:
                data = response.json()
                return data.get('items', [])
            else:
                print(f"GitHub API error: {response.status_code}")
                return []
                
        except Exception as e:
            print(f"Search error: {e}")
            return []
            
    def analyze_repo_quality(self, repo):
        """Analyze if repository meets elite criteria"""
        try:
            full_name = repo['full_name']
            
            # Basic criteria check
            stars = repo.get('stargazers_count', 0)
            if stars < 1000:
                return False
                
            # Get detailed repository info
            repo_url = f"https://api.github.com/repos/{full_name}"
            response = requests.get(repo_url, headers=self.get_github_headers())
            
            if response.status_code != 200:
                return False
                
            repo_data = response.json()
            
            # Check age (12+ months)
            created_at = datetime.fromisoformat(repo_data['created_at'].replace('Z', '+00:00'))
            age_months = (datetime.now(created_at.tzinfo) - created_at).days / 30
            
            if age_months < 12:
                return False
                
            # Check recent activity
            updated_at = datetime.fromisoformat(repo_data['updated_at'].replace('Z', '+00:00'))
            days_since_update = (datetime.now(updated_at.tzinfo) - updated_at).days
            
            if days_since_update > 90:  # No updates in 3 months
                return False
                
            # Get contributors count
            contributors_url = f"https://api.github.com/repos/{full_name}/contributors"
            contrib_response = requests.get(contributors_url, headers=self.get_github_headers())
            
            contributors_count = 0
            if contrib_response.status_code == 200:
                contributors_count = len(contrib_response.json())
                
            # Check commit activity
            commits_url = f"https://api.github.com/repos/{full_name}/commits"
            commits_response = requests.get(commits_url, params={'per_page': 100}, headers=self.get_github_headers())
            
            recent_commits = 0
            if commits_response.status_code == 200:
                commits = commits_response.json()
                cutoff_date = datetime.now() - timedelta(days=30)
                
                for commit in commits:
                    commit_date = datetime.fromisoformat(commit['commit']['author']['date'].replace('Z', '+00:00'))
                    if commit_date > cutoff_date:
                        recent_commits += 1
                        
            # Elite criteria scoring
            score = 0
            if stars >= 5000: score += 3
            elif stars >= 2000: score += 2
            elif stars >= 1000: score += 1
            
            if contributors_count >= 50: score += 3
            elif contributors_count >= 20: score += 2
            elif contributors_count >= 10: score += 1
            
            if recent_commits >= 20: score += 3
            elif recent_commits >= 10: score += 2
            elif recent_commits >= 5: score += 1
            
            if age_months >= 36: score += 2  # 3+ years
            elif age_months >= 24: score += 1  # 2+ years
            
            # Must score at least 6/11 to be considered elite
            if score >= 6:
                elite_repo = {
                    'full_name': full_name,
                    'stars': stars,
                    'contributors': contributors_count,
                    'age_months': age_months,
                    'recent_commits': recent_commits,
                    'score': score,
                    'language': repo_data.get('language', ''),
                    'description': repo_data.get('description', ''),
                    'topics': repo_data.get('topics', []),
                    'analyzed_at': datetime.now().isoformat()
                }
                
                self.elite_repos[full_name] = elite_repo
                print(f"✅ Elite repo found: {full_name} (Score: {score}/11)")
                
                # Immediately start learning from this repo
                self.learn_from_elite_repo(elite_repo)
                
                return True
                
        except Exception as e:
            print(f"Quality analysis error for {repo.get('full_name', 'unknown')}: {e}")
            
        return False
        
    def learn_from_elite_repo(self, repo):
        """Learn patterns from an elite repository"""
        try:
            full_name = repo['full_name']
            print(f"📚 Learning from: {full_name}")
            
            # Get repository structure
            structure = self.analyze_repo_structure(full_name)
            
            # Get README patterns
            readme_patterns = self.analyze_readme(full_name)
            
            # Get code patterns from main files
            code_patterns = self.analyze_code_patterns(full_name)
            
            # Get contribution patterns
            contribution_patterns = self.analyze_contribution_patterns(full_name)
            
            # Store knowledge
            self.knowledge_base[full_name] = {
                'repo_info': repo,
                'structure': structure,
                'readme_patterns': readme_patterns,
                'code_patterns': code_patterns,
                'contribution_patterns': contribution_patterns,
                'learned_at': datetime.now().isoformat()
            }
            
        except Exception as e:
            print(f"Learning error for {full_name}: {e}")
            
    def analyze_repo_structure(self, full_name):
        """Analyze repository structure patterns"""
        try:
            # Get repository contents
            url = f"https://api.github.com/repos/{full_name}/contents"
            response = requests.get(url, headers=self.get_github_headers())
            
            if response.status_code != 200:
                return {}
                
            contents = response.json()
            
            structure = {
                'root_files': [],
                'directories': [],
                'has_tests': False,
                'has_docs': False,
                'has_ci': False,
                'has_docker': False
            }
            
            for item in contents:
                if item['type'] == 'file':
                    structure['root_files'].append(item['name'])
                    
                    # Check for important files
                    name_lower = item['name'].lower()
                    if 'test' in name_lower:
                        structure['has_tests'] = True
                    elif name_lower in ['dockerfile', 'docker-compose.yml']:
                        structure['has_docker'] = True
                        
                elif item['type'] == 'dir':
                    structure['directories'].append(item['name'])
                    
                    # Check for important directories
                    name_lower = item['name'].lower()
                    if name_lower in ['tests', 'test', '__tests__']:
                        structure['has_tests'] = True
                    elif name_lower in ['docs', 'documentation']:
                        structure['has_docs'] = True
                    elif name_lower in ['.github', '.gitlab-ci', 'ci']:
                        structure['has_ci'] = True
                        
            return structure
            
        except Exception as e:
            return {}
            
    def analyze_readme(self, full_name):
        """Analyze README patterns for best practices"""
        try:
            url = f"https://api.github.com/repos/{full_name}/readme"
            response = requests.get(url, headers=self.get_github_headers())
            
            if response.status_code != 200:
                return {}
                
            readme_data = response.json()
            
            # Decode content
            import base64
            content = base64.b64decode(readme_data['content']).decode('utf-8')
            
            patterns = {
                'has_badges': bool(re.search(r'!\[.*\]\(.*badge.*\)', content)),
                'has_installation': 'install' in content.lower(),
                'has_usage_examples': 'example' in content.lower() or 'usage' in content.lower(),
                'has_contributing': 'contribut' in content.lower(),
                'has_license': 'license' in content.lower(),
                'has_toc': 'table of contents' in content.lower() or '- [' in content,
                'sections_count': len(re.findall(r'^#+\s', content, re.MULTILINE)),
                'code_blocks_count': content.count('```'),
                'length': len(content)
            }
            
            return patterns
            
        except Exception as e:
            return {}
            
    def analyze_code_patterns(self, full_name):
        """Analyze code patterns from main files"""
        try:
            patterns = {
                'languages': {},
                'common_patterns': [],
                'error_handling': [],
                'testing_patterns': [],
                'documentation_style': ''
            }
            
            # Get main files (limited to avoid rate limits)
            url = f"https://api.github.com/repos/{full_name}/contents"
            response = requests.get(url, headers=self.get_github_headers())
            
            if response.status_code != 200:
                return patterns
                
            contents = response.json()
            
            # Analyze a few key files
            key_files = [item for item in contents if item['type'] == 'file' and 
                        any(item['name'].endswith(ext) for ext in ['.py', '.js', '.cs', '.ts', '.go', '.rs'])]
            
            for file_item in key_files[:5]:  # Limit to 5 files
                try:
                    file_url = file_item['download_url']
                    file_response = requests.get(file_url)
                    
                    if file_response.status_code == 200:
                        code_content = file_response.text
                        self.extract_code_patterns_from_content(code_content, file_item['name'], patterns)
                        
                except Exception as e:
                    continue
                    
            return patterns
            
        except Exception as e:
            return {}
            
    def extract_code_patterns_from_content(self, content, filename, patterns):
        """Extract patterns from code content"""
        extension = os.path.splitext(filename)[1]
        
        # Language tracking
        if extension not in patterns['languages']:
            patterns['languages'][extension] = 0
        patterns['languages'][extension] += 1
        
        # Error handling patterns
        if 'try:' in content or 'except' in content or 'finally:' in content:
            patterns['error_handling'].append('Python try/except')
        elif 'try {' in content and 'catch' in content:
            patterns['error_handling'].append('JavaScript/C# try/catch')
            
        # Testing patterns
        if 'test_' in content or 'def test' in content:
            patterns['testing_patterns'].append('pytest/unittest style')
        elif 'it(' in content or 'describe(' in content:
            patterns['testing_patterns'].append('Jest/Mocha style')
        elif '[Test]' in content or '[TestMethod]' in content:
            patterns['testing_patterns'].append('NUnit/MSTest style')
            
        # Documentation style
        if '"""' in content:
            patterns['documentation_style'] = 'Python docstrings'
        elif '///' in content:
            patterns['documentation_style'] = 'XML documentation'
        elif '/* */' in content:
            patterns['documentation_style'] = 'Block comments'
            
    def analyze_contribution_patterns(self, full_name):
        """Analyze contribution and maintenance patterns"""
        try:
            # Get recent issues and PRs
            issues_url = f"https://api.github.com/repos/{full_name}/issues"
            response = requests.get(issues_url, params={'state': 'all', 'per_page': 50}, headers=self.get_github_headers())
            
            patterns = {
                'avg_issue_response_time': 0,
                'issue_labels_used': [],
                'pr_merge_rate': 0,
                'maintenance_activity': 'active'
            }
            
            if response.status_code == 200:
                issues = response.json()
                
                # Analyze issue patterns
                for issue in issues:
                    if 'labels' in issue:
                        for label in issue['labels']:
                            if label['name'] not in patterns['issue_labels_used']:
                                patterns['issue_labels_used'].append(label['name'])
                                
            return patterns
            
        except Exception as e:
            return {}
            
    def analyze_elite_patterns(self):
        """Analyze patterns across all elite repositories"""
        while self.running:
            try:
                if len(self.knowledge_base) < 5:
                    time.sleep(30)
                    continue
                    
                print("🧠 Analyzing patterns across elite repositories...")
                
                # Common structure patterns
                self.find_common_structure_patterns()
                
                # Best practice patterns
                self.find_best_practice_patterns()
                
                # Save patterns
                self.save_patterns()
                
                time.sleep(600)  # Re-analyze every 10 minutes
                
            except Exception as e:
                print(f"Pattern analysis error: {e}")
                time.sleep(60)
                
    def find_common_structure_patterns(self):
        """Find common repository structure patterns"""
        structure_patterns = {}
        
        for repo_name, knowledge in self.knowledge_base.items():
            structure = knowledge.get('structure', {})
            
            # Count common directories
            for directory in structure.get('directories', []):
                if directory not in structure_patterns:
                    structure_patterns[directory] = 0
                structure_patterns[directory] += 1
                
        # Find most common patterns (appear in >20% of repos)
        min_count = max(1, len(self.knowledge_base) * 0.2)
        common_patterns = {k: v for k, v in structure_patterns.items() if v >= min_count}
        
        self.patterns['common_directories'] = common_patterns
        
    def find_best_practice_patterns(self):
        """Find best practice patterns"""
        practices = {
            'has_tests': 0,
            'has_docs': 0,
            'has_ci': 0,
            'has_docker': 0,
            'good_readme': 0
        }
        
        for repo_name, knowledge in self.knowledge_base.items():
            structure = knowledge.get('structure', {})
            readme = knowledge.get('readme_patterns', {})
            
            if structure.get('has_tests'): practices['has_tests'] += 1
            if structure.get('has_docs'): practices['has_docs'] += 1
            if structure.get('has_ci'): practices['has_ci'] += 1
            if structure.get('has_docker'): practices['has_docker'] += 1
            
            # Good README criteria
            if (readme.get('has_installation') and 
                readme.get('has_usage_examples') and 
                readme.get('sections_count', 0) >= 5):
                practices['good_readme'] += 1
                
        total_repos = len(self.knowledge_base)
        self.best_practices = {k: v/total_repos for k, v in practices.items()}
        
    def save_elite_repos(self):
        """Save elite repositories list"""
        with open('/Users/jamestunick/xrai/elite_repos.json', 'w') as f:
            json.dump(self.elite_repos, f, indent=2)
            
    def save_patterns(self):
        """Save learned patterns"""
        with open('/Users/jamestunick/xrai/elite_patterns.json', 'w') as f:
            json.dump({
                'patterns': self.patterns,
                'best_practices': self.best_practices,
                'last_updated': datetime.now().isoformat()
            }, f, indent=2)
            
    def get_recommendation_for_project(self, project_type):
        """Get recommendations based on elite repository patterns"""
        recommendations = []
        
        # Structure recommendations
        if 'common_directories' in self.patterns:
            recommendations.append("Common directory structure:")
            for dir_name, count in sorted(self.patterns['common_directories'].items(), key=lambda x: x[1], reverse=True)[:5]:
                percentage = count / len(self.knowledge_base) * 100
                recommendations.append(f"  {dir_name}/ (used by {percentage:.0f}% of elite repos)")
                
        # Best practice recommendations
        if self.best_practices:
            recommendations.append("\nBest practices from elite repos:")
            for practice, percentage in self.best_practices.items():
                if percentage > 0.5:  # More than 50% use this practice
                    recommendations.append(f"  ✅ {practice.replace('_', ' ').title()}: {percentage:.0%} adoption")
                    
        return recommendations

if __name__ == "__main__":
    print("🌟 Starting Elite Repositories Knowledge Agent...")
    print("🔍 Learning from battle-tested repositories (1000+ stars, active community)...")
    
    agent = EliteReposAgent()
    
    try:
        while True:
            print(f"\n📊 Knowledge Status:")
            print(f"  Elite repos discovered: {len(agent.elite_repos)}")
            print(f"  Repos analyzed: {len(agent.knowledge_base)}")
            print(f"  Pattern categories: {len(agent.patterns)}")
            
            time.sleep(120)  # Status update every 2 minutes
            
    except KeyboardInterrupt:
        agent.running = False
        print("\n🛑 Elite Repositories Agent stopped")