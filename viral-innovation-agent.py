#!/usr/bin/env python3
"""
Viral Innovation Agent - Detects breakthrough technical achievements
Identifies rapidly growing repos, viral innovations, and breakthrough patterns
Focuses on code quality metrics that make repositories successful
"""
import subprocess
import json
import os
import threading
import time
from datetime import datetime, timedelta
import requests
import re
import statistics

class ViralInnovationAgent:
    def __init__(self):
        self.viral_repos = {}
        self.innovation_patterns = {}
        self.breakthrough_indicators = {}
        self.quality_metrics = {}
        self.running = True
        
        # GitHub API token
        self.github_token = os.environ.get('GITHUB_TOKEN', '')
        
        # Start detection threads
        threading.Thread(target=self.detect_viral_repos, daemon=True).start()
        threading.Thread(target=self.analyze_breakthrough_patterns, daemon=True).start()
        threading.Thread(target=self.track_rapid_growth, daemon=True).start()
        
    def get_github_headers(self):
        """Get headers for GitHub API requests"""
        headers = {
            'Accept': 'application/vnd.github.v3+json',
            'User-Agent': 'XRAI-Viral-Innovation-Agent'
        }
        if self.github_token:
            headers['Authorization'] = f'token {self.github_token}'
        return headers
        
    def detect_viral_repos(self):
        """Detect rapidly growing and viral repositories"""
        while self.running:
            try:
                # Search for recently viral repos
                viral_queries = [
                    # Recently created with high stars
                    'created:>2024-01-01+stars:>1000',
                    
                    # AI/ML breakthroughs
                    'machine-learning+created:>2023-01-01+stars:>500',
                    'artificial-intelligence+created:>2023-01-01+stars:>500',
                    'llm+created:>2023-01-01+stars:>300',
                    'chatgpt+created:>2023-01-01+stars:>300',
                    
                    # Developer tools breakthroughs
                    'developer-tools+created:>2023-01-01+stars:>500',
                    'productivity+created:>2023-01-01+stars:>500',
                    'automation+created:>2023-01-01+stars:>500',
                    
                    # Unity/VR innovations
                    'unity+created:>2023-01-01+stars:>300',
                    'vr+created:>2023-01-01+stars:>300',
                    'ar+created:>2023-01-01+stars:>300',
                    
                    # Web/Visualization breakthroughs
                    'visualization+created:>2023-01-01+stars:>500',
                    'ui+created:>2023-01-01+stars:>500',
                    'web-components+created:>2023-01-01+stars:>300'
                ]
                
                for query in viral_queries:
                    print(f"🔥 Searching viral repos: {query}")
                    repos = self.search_repos_by_query(query)
                    
                    for repo in repos:
                        if self.is_viral_innovation(repo):
                            self.analyze_viral_repo(repo)
                            
                    time.sleep(2)  # Rate limiting
                    
                # Look for trending repositories
                self.check_trending_repos()
                
                time.sleep(1800)  # Check every 30 minutes
                
            except Exception as e:
                print(f"Viral detection error: {e}")
                time.sleep(300)
                
    def search_repos_by_query(self, query):
        """Search repositories by query"""
        try:
            url = "https://api.github.com/search/repositories"
            params = {
                'q': query,
                'sort': 'stars',
                'order': 'desc',
                'per_page': 30
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
            
    def is_viral_innovation(self, repo):
        """Determine if repository shows viral innovation characteristics"""
        try:
            full_name = repo['full_name']
            stars = repo.get('stargazers_count', 0)
            created_at = datetime.fromisoformat(repo['created_at'].replace('Z', '+00:00'))
            
            # Calculate age in days
            age_days = (datetime.now(created_at.tzinfo) - created_at).days
            
            # Viral indicators
            viral_score = 0
            
            # Rapid star growth
            if age_days > 0:
                stars_per_day = stars / age_days
                
                if stars_per_day >= 50:  # 50+ stars per day
                    viral_score += 5
                elif stars_per_day >= 20:  # 20+ stars per day
                    viral_score += 3
                elif stars_per_day >= 10:  # 10+ stars per day
                    viral_score += 2
                elif stars_per_day >= 5:   # 5+ stars per day
                    viral_score += 1
                    
            # Innovation keywords in description
            description = repo.get('description', '').lower()
            innovation_keywords = [
                'breakthrough', 'revolutionary', 'game-changing', 'innovative',
                'novel', 'cutting-edge', 'first-of-its-kind', 'paradigm',
                'simplifies', 'reimagines', 'reinvents', 'transforms'
            ]
            
            for keyword in innovation_keywords:
                if keyword in description:
                    viral_score += 1
                    
            # Technical achievement indicators
            tech_keywords = [
                'zero-config', 'one-line', 'blazing-fast', 'ultra-light',
                'memory-efficient', 'real-time', 'instant', 'seamless',
                'drop-in', 'plug-and-play', 'minimal', 'lightweight'
            ]
            
            for keyword in tech_keywords:
                if keyword in description:
                    viral_score += 1
                    
            # Language/framework innovations
            if repo.get('language') in ['Rust', 'Go', 'TypeScript', 'Python']:
                viral_score += 1
                
            # Must have minimum stars and viral score
            return stars >= 100 and viral_score >= 3
            
        except Exception as e:
            return False
            
    def analyze_viral_repo(self, repo):
        """Analyze what makes a viral repository successful"""
        try:
            full_name = repo['full_name']
            
            if full_name in self.viral_repos:
                return  # Already analyzed
                
            print(f"🚀 Analyzing viral repo: {full_name}")
            
            # Get detailed repository data
            repo_url = f"https://api.github.com/repos/{full_name}"
            response = requests.get(repo_url, headers=self.get_github_headers())
            
            if response.status_code != 200:
                return
                
            detailed_repo = response.json()
            
            # Analyze quality metrics
            quality_metrics = self.analyze_code_quality(full_name)
            
            # Analyze innovation factors
            innovation_factors = self.analyze_innovation_factors(detailed_repo)
            
            # Analyze growth pattern
            growth_pattern = self.analyze_growth_pattern(full_name)
            
            # Store viral repository data
            self.viral_repos[full_name] = {
                'repo_data': detailed_repo,
                'quality_metrics': quality_metrics,
                'innovation_factors': innovation_factors,
                'growth_pattern': growth_pattern,
                'analyzed_at': datetime.now().isoformat(),
                'viral_score': self.calculate_viral_score(detailed_repo, quality_metrics, innovation_factors)
            }
            
        except Exception as e:
            print(f"Viral analysis error for {full_name}: {e}")
            
    def analyze_code_quality(self, full_name):
        """Analyze code quality metrics that contribute to success"""
        try:
            metrics = {
                'readme_quality': 0,
                'code_structure': 0,
                'documentation_coverage': 0,
                'test_coverage': 0,
                'simplicity_score': 0,
                'maintainability': 0
            }
            
            # Analyze README quality
            readme_score = self.analyze_readme_quality(full_name)
            metrics['readme_quality'] = readme_score
            
            # Analyze repository structure
            structure_score = self.analyze_repo_structure_quality(full_name)
            metrics['code_structure'] = structure_score
            
            # Check for tests
            test_score = self.check_test_presence(full_name)
            metrics['test_coverage'] = test_score
            
            # Analyze main code files for simplicity
            simplicity_score = self.analyze_code_simplicity(full_name)
            metrics['simplicity_score'] = simplicity_score
            
            return metrics
            
        except Exception as e:
            return {}
            
    def analyze_readme_quality(self, full_name):
        """Analyze README quality (critical for viral success)"""
        try:
            url = f"https://api.github.com/repos/{full_name}/readme"
            response = requests.get(url, headers=self.get_github_headers())
            
            if response.status_code != 200:
                return 0
                
            readme_data = response.json()
            import base64
            content = base64.b64decode(readme_data['content']).decode('utf-8')
            
            score = 0
            
            # Visual appeal indicators
            if re.search(r'!\[.*\]\(.*\.(png|jpg|gif|svg)', content):
                score += 2  # Has images/demos
                
            # Clear structure
            headers = re.findall(r'^#+\s', content, re.MULTILINE)
            if len(headers) >= 3:
                score += 2  # Good structure
                
            # Quick start/installation
            if re.search(r'(quick.?start|installation|getting.?started)', content, re.IGNORECASE):
                score += 2
                
            # Code examples
            code_blocks = content.count('```')
            if code_blocks >= 2:
                score += 2  # Has code examples
                
            # Badges (credibility)
            if re.search(r'!\[.*\]\(.*badge', content):
                score += 1
                
            # Demo/live links
            if re.search(r'(demo|live|try.?it)', content, re.IGNORECASE):
                score += 2
                
            return min(score, 10)  # Max score of 10
            
        except Exception as e:
            return 0
            
    def analyze_repo_structure_quality(self, full_name):
        """Analyze repository structure for maintainability"""
        try:
            url = f"https://api.github.com/repos/{full_name}/contents"
            response = requests.get(url, headers=self.get_github_headers())
            
            if response.status_code != 200:
                return 0
                
            contents = response.json()
            
            score = 0
            files = [item['name'] for item in contents if item['type'] == 'file']
            dirs = [item['name'] for item in contents if item['type'] == 'dir']
            
            # Clean root directory (not cluttered)
            if len(files) <= 10:
                score += 2
                
            # Important files present
            important_files = ['LICENSE', 'CHANGELOG', 'CONTRIBUTING', '.gitignore']
            for file in important_files:
                if any(f.upper().startswith(file.upper()) for f in files):
                    score += 1
                    
            # Good directory structure
            good_dirs = ['src', 'lib', 'docs', 'examples', 'tests', 'scripts']
            for dir_name in good_dirs:
                if any(d.lower() == dir_name for d in dirs):
                    score += 1
                    
            return min(score, 10)
            
        except Exception as e:
            return 0
            
    def check_test_presence(self, full_name):
        """Check for test presence (indicates quality)"""
        try:
            # Search for test files
            url = f"https://api.github.com/search/code"
            params = {
                'q': f'repo:{full_name} filename:test',
                'per_page': 5
            }
            
            response = requests.get(url, params=params, headers=self.get_github_headers())
            
            if response.status_code == 200:
                data = response.json()
                test_files = data.get('total_count', 0)
                return min(test_files * 2, 10)  # Up to 10 points
            else:
                return 0
                
        except Exception as e:
            return 0
            
    def analyze_code_simplicity(self, full_name):
        """Analyze code for simplicity (key viral factor)"""
        try:
            # Get main files
            url = f"https://api.github.com/repos/{full_name}/contents"
            response = requests.get(url, headers=self.get_github_headers())
            
            if response.status_code != 200:
                return 0
                
            contents = response.json()
            score = 0
            
            # Look for main entry point
            main_files = []
            for item in contents:
                if item['type'] == 'file':
                    name_lower = item['name'].lower()
                    if (name_lower in ['main.py', 'index.js', 'app.py', 'main.go', 'lib.rs'] or
                        name_lower.startswith('main.') or 
                        name_lower == 'package.json'):
                        main_files.append(item)
                        
            # Analyze main files for simplicity indicators
            for file_item in main_files[:3]:  # Max 3 files
                try:
                    if 'download_url' in file_item:
                        file_response = requests.get(file_item['download_url'])
                        if file_response.status_code == 200:
                            content = file_response.text
                            
                            # Simplicity indicators
                            lines = content.count('\n')
                            if lines < 100:  # Short and simple
                                score += 3
                            elif lines < 300:
                                score += 2
                            elif lines < 500:
                                score += 1
                                
                            # Minimal dependencies
                            import_count = len(re.findall(r'^(import|require|use|#include)', content, re.MULTILINE))
                            if import_count < 5:
                                score += 2
                            elif import_count < 10:
                                score += 1
                                
                except Exception as e:
                    continue
                    
            return min(score, 10)
            
        except Exception as e:
            return 0
            
    def analyze_innovation_factors(self, repo_data):
        """Analyze what makes this repository innovative"""
        factors = {
            'novelty_score': 0,
            'problem_solving': 0,
            'simplification': 0,
            'performance_focus': 0,
            'developer_experience': 0
        }
        
        description = repo_data.get('description', '').lower()
        readme_topics = repo_data.get('topics', [])
        
        # Novelty indicators
        novelty_keywords = ['first', 'new', 'novel', 'innovative', 'breakthrough', 'revolutionary']
        factors['novelty_score'] = sum(1 for kw in novelty_keywords if kw in description)
        
        # Problem-solving focus
        problem_keywords = ['solves', 'fixes', 'addresses', 'eliminates', 'prevents', 'avoids']
        factors['problem_solving'] = sum(1 for kw in problem_keywords if kw in description)
        
        # Simplification focus
        simple_keywords = ['simple', 'easy', 'minimal', 'lightweight', 'clean', 'elegant']
        factors['simplification'] = sum(1 for kw in simple_keywords if kw in description)
        
        # Performance focus
        perf_keywords = ['fast', 'efficient', 'optimized', 'blazing', 'instant', 'real-time']
        factors['performance_focus'] = sum(1 for kw in perf_keywords if kw in description)
        
        # Developer experience
        dx_keywords = ['zero-config', 'plug-and-play', 'drop-in', 'out-of-the-box', 'just works']
        factors['developer_experience'] = sum(1 for kw in dx_keywords if kw in description)
        
        return factors
        
    def analyze_growth_pattern(self, full_name):
        """Analyze growth pattern (requires GitHub API access)"""
        try:
            # Get stargazers timeline (simplified)
            url = f"https://api.github.com/repos/{full_name}/stargazers"
            response = requests.get(url, params={'per_page': 100}, headers=self.get_github_headers())
            
            if response.status_code == 200:
                stargazers = response.json()
                
                # Simple growth analysis
                total_stars = len(stargazers)
                
                if total_stars >= 50:
                    return {
                        'growth_type': 'viral' if total_stars >= 1000 else 'rapid',
                        'total_stars': total_stars,
                        'growth_rate': 'high'
                    }
                    
            return {'growth_type': 'steady', 'total_stars': 0, 'growth_rate': 'normal'}
            
        except Exception as e:
            return {}
            
    def calculate_viral_score(self, repo_data, quality_metrics, innovation_factors):
        """Calculate overall viral potential score"""
        score = 0
        
        # Quality contribution (40%)
        quality_score = sum(quality_metrics.values()) / len(quality_metrics) if quality_metrics else 0
        score += quality_score * 0.4
        
        # Innovation contribution (35%)
        innovation_score = sum(innovation_factors.values()) / len(innovation_factors) if innovation_factors else 0
        score += innovation_score * 0.35
        
        # Stars contribution (15%)
        stars = repo_data.get('stargazers_count', 0)
        star_score = min(stars / 1000, 10)  # Max 10 points for 1000+ stars
        score += star_score * 0.15
        
        # Age factor (10%) - newer repos get bonus for rapid growth
        created_at = datetime.fromisoformat(repo_data['created_at'].replace('Z', '+00:00'))
        age_days = (datetime.now(created_at.tzinfo) - created_at).days
        age_score = max(0, 10 - (age_days / 30))  # Bonus for repos under 10 months
        score += age_score * 0.1
        
        return score
        
    def analyze_breakthrough_patterns(self):
        """Analyze patterns across breakthrough repositories"""
        while self.running:
            try:
                if len(self.viral_repos) < 3:
                    time.sleep(60)
                    continue
                    
                print("🧠 Analyzing breakthrough patterns...")
                
                # Find common success patterns
                self.find_viral_success_patterns()
                
                # Identify breakthrough indicators
                self.identify_breakthrough_indicators()
                
                # Save patterns
                self.save_viral_patterns()
                
                time.sleep(900)  # Re-analyze every 15 minutes
                
            except Exception as e:
                print(f"Pattern analysis error: {e}")
                time.sleep(300)
                
    def find_viral_success_patterns(self):
        """Find common patterns in viral repositories"""
        patterns = {
            'common_qualities': {},
            'success_factors': {},
            'technical_patterns': {}
        }
        
        high_scoring_repos = [repo for repo in self.viral_repos.values() if repo.get('viral_score', 0) > 7]
        
        if not high_scoring_repos:
            return
            
        # Analyze common qualities
        for repo in high_scoring_repos:
            quality_metrics = repo.get('quality_metrics', {})
            
            for metric, score in quality_metrics.items():
                if metric not in patterns['common_qualities']:
                    patterns['common_qualities'][metric] = []
                patterns['common_qualities'][metric].append(score)
                
        # Calculate averages
        for metric, scores in patterns['common_qualities'].items():
            patterns['common_qualities'][metric] = sum(scores) / len(scores)
            
        self.innovation_patterns = patterns
        
    def identify_breakthrough_indicators(self):
        """Identify early indicators of breakthrough potential"""
        indicators = []
        
        for repo_name, repo_data in self.viral_repos.items():
            score = repo_data.get('viral_score', 0)
            
            if score > 8:  # High viral score
                quality = repo_data.get('quality_metrics', {})
                innovation = repo_data.get('innovation_factors', {})
                
                # Strong README + Innovation = Breakthrough
                if quality.get('readme_quality', 0) > 7 and innovation.get('novelty_score', 0) > 2:
                    indicators.append("Strong README + Novel approach")
                    
                # Simplicity + Performance = Viral potential
                if quality.get('simplicity_score', 0) > 6 and innovation.get('performance_focus', 0) > 2:
                    indicators.append("Simple implementation + Performance focus")
                    
                # Great DX + Problem solving = Success
                if innovation.get('developer_experience', 0) > 2 and innovation.get('problem_solving', 0) > 2:
                    indicators.append("Excellent developer experience + Clear problem solving")
                    
        self.breakthrough_indicators = list(set(indicators))  # Remove duplicates
        
    def track_rapid_growth(self):
        """Track repositories showing rapid growth patterns"""
        while self.running:
            try:
                # Check recently created repos that might go viral
                recent_query = f"created:>{(datetime.now() - timedelta(days=30)).strftime('%Y-%m-%d')}+stars:>10"
                recent_repos = self.search_repos_by_query(recent_query)
                
                for repo in recent_repos:
                    # Track potential viral candidates
                    if self.has_viral_potential(repo):
                        print(f"👀 Tracking potential viral repo: {repo['full_name']}")
                        self.analyze_viral_repo(repo)
                        
                time.sleep(3600)  # Check every hour
                
            except Exception as e:
                print(f"Growth tracking error: {e}")
                time.sleep(600)
                
    def has_viral_potential(self, repo):
        """Check if repository has viral potential"""
        stars = repo.get('stargazers_count', 0)
        created_at = datetime.fromisoformat(repo['created_at'].replace('Z', '+00:00'))
        age_days = (datetime.now(created_at.tzinfo) - created_at).days
        
        if age_days == 0:
            age_days = 1  # Prevent division by zero
            
        stars_per_day = stars / age_days
        
        # Rapid growth indicators
        return (stars_per_day >= 2 and stars >= 10) or (stars >= 100 and age_days <= 30)
        
    def check_trending_repos(self):
        """Check GitHub trending for viral repositories"""
        try:
            # GitHub doesn't have a trending API, but we can simulate with recent popular repos
            trending_query = f"created:>{(datetime.now() - timedelta(days=7)).strftime('%Y-%m-%d')}+sort:stars"
            trending_repos = self.search_repos_by_query(trending_query)
            
            for repo in trending_repos[:10]:  # Top 10 trending
                if repo.get('stargazers_count', 0) >= 50:  # Minimum threshold
                    self.analyze_viral_repo(repo)
                    
        except Exception as e:
            print(f"Trending check error: {e}")
            
    def save_viral_patterns(self):
        """Save viral patterns and insights"""
        with open('/Users/jamestunick/xrai/viral_patterns.json', 'w') as f:
            json.dump({
                'viral_repos': self.viral_repos,
                'innovation_patterns': self.innovation_patterns,
                'breakthrough_indicators': self.breakthrough_indicators,
                'last_updated': datetime.now().isoformat()
            }, f, indent=2)
            
    def get_viral_recommendations(self, project_type="general"):
        """Get recommendations based on viral repository analysis"""
        recommendations = []
        
        if self.innovation_patterns:
            avg_qualities = self.innovation_patterns.get('common_qualities', {})
            
            recommendations.append("🚀 Viral Success Patterns:")
            
            # README quality is critical
            readme_score = avg_qualities.get('readme_quality', 0)
            if readme_score > 7:
                recommendations.append("  ✅ High-quality README with visuals and examples (Average: {:.1f}/10)".format(readme_score))
                
            # Simplicity drives adoption
            simplicity_score = avg_qualities.get('simplicity_score', 0)
            if simplicity_score > 6:
                recommendations.append("  ✅ Keep implementation simple and minimal (Average: {:.1f}/10)".format(simplicity_score))
                
        if self.breakthrough_indicators:
            recommendations.append("\n🎯 Breakthrough Indicators:")
            for indicator in self.breakthrough_indicators:
                recommendations.append(f"  • {indicator}")
                
        # General viral principles
        recommendations.extend([
            "\n💡 Viral Repository Principles:",
            "  • Solve a real problem simply",
            "  • Amazing README with GIFs/demos",
            "  • Zero-config, works immediately",
            "  • Clear value proposition",
            "  • Excellent developer experience",
            "  • Performance benefits",
            "  • Clean, readable code"
        ])
        
        return recommendations

if __name__ == "__main__":
    print("🔥 Starting Viral Innovation Agent...")
    print("🚀 Detecting breakthrough repositories and viral patterns...")
    
    agent = ViralInnovationAgent()
    
    try:
        while True:
            print(f"\n📊 Innovation Status:")
            print(f"  Viral repos analyzed: {len(agent.viral_repos)}")
            print(f"  Breakthrough indicators: {len(agent.breakthrough_indicators)}")
            print(f"  Pattern categories: {len(agent.innovation_patterns)}")
            
            # Show recent discoveries
            recent_viral = [name for name, data in agent.viral_repos.items() 
                          if datetime.fromisoformat(data['analyzed_at']) > datetime.now() - timedelta(hours=1)]
            if recent_viral:
                print(f"  🆕 Recent discoveries: {', '.join(recent_viral[:3])}")
                
            time.sleep(180)  # Status update every 3 minutes
            
    except KeyboardInterrupt:
        agent.running = False
        print("\n🛑 Viral Innovation Agent stopped")