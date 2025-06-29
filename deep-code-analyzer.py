#!/usr/bin/env python3
"""
Deep Code Analyzer - Analyzes code quality beyond stars
Uses benchmarks, leaderboards, and deep code analysis to identify truly superior implementations
"""
import subprocess
import json
import os
import threading
import time
from datetime import datetime, timedelta
import requests
import re
import ast
import radon.complexity as radon_cc
import radon.metrics as radon_metrics

class DeepCodeAnalyzer:
    def __init__(self):
        self.benchmark_data = {}
        self.leaderboard_repos = {}
        self.code_quality_metrics = {}
        self.performance_data = {}
        self.sota_comparisons = {}
        self.running = True
        
        # Data sources
        self.data_sources = {
            'huggingface_leaderboards': 'https://huggingface.co/spaces/HuggingFaceH4/open_llm_leaderboard',
            'papers_with_code': 'https://paperswithcode.com/api/v1/',
            'ml_benchmarks': 'https://paperswithcode.com/api/v1/benchmarks/',
            'github_api': 'https://api.github.com/repos/'
        }
        
        # Start analysis threads
        threading.Thread(target=self.monitor_leaderboards, daemon=True).start()
        threading.Thread(target=self.analyze_benchmark_repos, daemon=True).start()
        threading.Thread(target=self.deep_code_analysis, daemon=True).start()
        
    def monitor_leaderboards(self):
        """Monitor various AI/ML leaderboards for top-performing repositories"""
        while self.running:
            try:
                print("📊 Monitoring leaderboards for top performers...")
                
                # Papers with Code leaderboards
                self.fetch_papers_with_code_leaders()
                
                # Hugging Face leaderboards
                self.fetch_huggingface_leaders()
                
                # GitHub trending in ML
                self.fetch_github_ml_trending()
                
                # Benchmark-specific searches
                self.search_benchmark_repos()
                
                time.sleep(1800)  # Check every 30 minutes
                
            except Exception as e:
                print(f"Leaderboard monitoring error: {e}")
                time.sleep(300)
                
    def fetch_papers_with_code_leaders(self):
        """Fetch top repositories from Papers with Code"""
        try:
            # Get popular benchmarks
            url = "https://paperswithcode.com/api/v1/benchmarks/"
            response = requests.get(url, params={'page': 1})
            
            if response.status_code == 200:
                benchmarks = response.json()
                
                for benchmark in benchmarks.get('results', [])[:10]:  # Top 10 benchmarks
                    benchmark_name = benchmark.get('name', '')
                    benchmark_id = benchmark.get('id', '')
                    
                    # Get papers for this benchmark
                    papers_url = f"https://paperswithcode.com/api/v1/papers/"
                    papers_response = requests.get(papers_url, params={
                        'benchmark': benchmark_id,
                        'page': 1
                    })
                    
                    if papers_response.status_code == 200:
                        papers_data = papers_response.json()
                        
                        for paper in papers_data.get('results', [])[:5]:  # Top 5 papers
                            self.analyze_paper_repositories(paper, benchmark_name)
                            
                    time.sleep(1)  # Rate limiting
                    
        except Exception as e:
            print(f"Papers with Code fetch error: {e}")
            
    def analyze_paper_repositories(self, paper, benchmark_name):
        """Analyze repositories associated with research papers"""
        try:
            paper_title = paper.get('title', '')
            paper_url = paper.get('url_pdf', '')
            
            # Look for GitHub links in the paper
            if 'repository' in paper:
                repo_url = paper['repository']
                if 'github.com' in repo_url:
                    repo_path = repo_url.replace('https://github.com/', '').replace('.git', '')
                    
                    self.leaderboard_repos[repo_path] = {
                        'source': 'papers_with_code',
                        'benchmark': benchmark_name,
                        'paper_title': paper_title,
                        'paper_url': paper_url,
                        'found_at': datetime.now().isoformat()
                    }
                    
                    # Queue for deep analysis
                    self.queue_for_analysis(repo_path, 'benchmark_leader')
                    
        except Exception as e:
            print(f"Paper repository analysis error: {e}")
            
    def fetch_huggingface_leaders(self):
        """Fetch top models and their repositories from Hugging Face"""
        try:
            # Get trending models
            url = "https://huggingface.co/api/models"
            params = {
                'sort': 'downloads',
                'direction': -1,
                'limit': 50
            }
            
            response = requests.get(url, params=params)
            
            if response.status_code == 200:
                models = response.json()
                
                for model in models:
                    model_id = model.get('id', '')
                    downloads = model.get('downloads', 0)
                    
                    # Get model details for repository links
                    model_url = f"https://huggingface.co/api/models/{model_id}"
                    model_response = requests.get(model_url)
                    
                    if model_response.status_code == 200:
                        model_data = model_response.json()
                        
                        # Look for GitHub repository
                        if 'repository' in model_data or 'github' in str(model_data).lower():
                            self.extract_github_from_model(model_data, downloads)
                            
                    time.sleep(0.5)  # Rate limiting
                    
        except Exception as e:
            print(f"Hugging Face fetch error: {e}")
            
    def extract_github_from_model(self, model_data, downloads):
        """Extract GitHub repository from Hugging Face model data"""
        try:
            # Check various fields for GitHub links
            text_content = json.dumps(model_data).lower()
            github_urls = re.findall(r'github\.com/([^/\s"]+/[^/\s"]+)', text_content)
            
            for repo_path in github_urls:
                if repo_path not in self.leaderboard_repos:
                    self.leaderboard_repos[repo_path] = {
                        'source': 'huggingface',
                        'model_id': model_data.get('id', ''),
                        'downloads': downloads,
                        'found_at': datetime.now().isoformat()
                    }
                    
                    self.queue_for_analysis(repo_path, 'hf_leader')
                    
        except Exception as e:
            print(f"GitHub extraction error: {e}")
            
    def fetch_github_ml_trending(self):
        """Fetch trending ML repositories from GitHub"""
        try:
            queries = [
                'machine-learning+sort:stars+language:python',
                'deep-learning+sort:stars+language:python', 
                'artificial-intelligence+sort:stars+language:python',
                'computer-vision+sort:stars+language:python',
                'natural-language-processing+sort:stars+language:python',
                'speech-recognition+sort:stars+language:python',
                'reinforcement-learning+sort:stars+language:python'
            ]
            
            for query in queries:
                url = "https://api.github.com/search/repositories"
                params = {'q': query, 'per_page': 20, 'sort': 'stars'}
                
                response = requests.get(url)
                if response.status_code == 200:
                    data = response.json()
                    
                    for repo in data.get('items', []):
                        repo_path = repo['full_name']
                        
                        if repo_path not in self.leaderboard_repos:
                            self.leaderboard_repos[repo_path] = {
                                'source': 'github_trending_ml',
                                'stars': repo.get('stargazers_count', 0),
                                'language': repo.get('language', ''),
                                'description': repo.get('description', ''),
                                'found_at': datetime.now().isoformat()
                            }
                            
                        self.queue_for_analysis(repo_path, 'trending_ml')
                        
                time.sleep(2)  # Rate limiting
                
        except Exception as e:
            print(f"GitHub ML trending error: {e}")
            
    def search_benchmark_repos(self):
        """Search for repositories with benchmark results"""
        try:
            benchmark_queries = [
                'benchmark+results+language:python+stars:>100',
                'evaluation+metrics+language:python+stars:>100', 
                'performance+comparison+language:python+stars:>100',
                'sota+state-of-the-art+language:python+stars:>100',
                'leaderboard+language:python+stars:>100'
            ]
            
            for query in benchmark_queries:
                url = "https://api.github.com/search/repositories"
                params = {'q': query, 'per_page': 30}
                
                response = requests.get(url)
                if response.status_code == 200:
                    data = response.json()
                    
                    for repo in data.get('items', []):
                        repo_path = repo['full_name']
                        self.queue_for_analysis(repo_path, 'benchmark_repo')
                        
                time.sleep(2)
                
        except Exception as e:
            print(f"Benchmark search error: {e}")
            
    def queue_for_analysis(self, repo_path, analysis_type):
        """Queue repository for deep analysis"""
        if repo_path not in self.code_quality_metrics:
            self.code_quality_metrics[repo_path] = {
                'analysis_type': analysis_type,
                'queued_at': datetime.now().isoformat(),
                'status': 'queued'
            }
            
    def analyze_benchmark_repos(self):
        """Analyze repositories that appear on benchmarks"""
        while self.running:
            try:
                # Find queued repositories
                queued_repos = [repo for repo, data in self.code_quality_metrics.items() 
                               if data.get('status') == 'queued']
                
                for repo_path in queued_repos[:5]:  # Analyze 5 at a time
                    print(f"🔍 Deep analyzing: {repo_path}")
                    self.perform_deep_analysis(repo_path)
                    time.sleep(10)  # Pause between analyses
                    
                time.sleep(300)  # Check queue every 5 minutes
                
            except Exception as e:
                print(f"Benchmark analysis error: {e}")
                time.sleep(60)
                
    def perform_deep_analysis(self, repo_path):
        """Perform comprehensive analysis of a repository"""
        try:
            self.code_quality_metrics[repo_path]['status'] = 'analyzing'
            
            # Clone repository temporarily for analysis
            temp_dir = f"/tmp/xrai_analysis_{repo_path.replace('/', '_')}"
            
            # Clone repo
            clone_result = subprocess.run([
                'git', 'clone', '--depth', '1', 
                f'https://github.com/{repo_path}', temp_dir
            ], capture_output=True, text=True)
            
            if clone_result.returncode != 0:
                self.code_quality_metrics[repo_path]['status'] = 'clone_failed'
                return
                
            # Perform various analyses
            analysis_results = {}
            
            # 1. Code complexity analysis
            analysis_results['complexity'] = self.analyze_code_complexity(temp_dir)
            
            # 2. Test coverage analysis
            analysis_results['test_coverage'] = self.analyze_test_coverage(temp_dir)
            
            # 3. Performance benchmarks (if available)
            analysis_results['benchmarks'] = self.find_benchmark_results(temp_dir)
            
            # 4. Documentation quality
            analysis_results['documentation'] = self.analyze_documentation_quality(temp_dir)
            
            # 5. Dependency analysis
            analysis_results['dependencies'] = self.analyze_dependencies(temp_dir)
            
            # 6. Architecture analysis
            analysis_results['architecture'] = self.analyze_architecture(temp_dir)
            
            # 7. Performance indicators
            analysis_results['performance_indicators'] = self.find_performance_indicators(temp_dir)
            
            # Store results
            self.code_quality_metrics[repo_path].update({
                'analysis_results': analysis_results,
                'analyzed_at': datetime.now().isoformat(),
                'status': 'completed'
            })
            
            # Cleanup
            subprocess.run(['rm', '-rf', temp_dir], capture_output=True)
            
            print(f"✅ Analysis completed: {repo_path}")
            
        except Exception as e:
            print(f"Deep analysis error for {repo_path}: {e}")
            self.code_quality_metrics[repo_path]['status'] = 'failed'
            
    def analyze_code_complexity(self, repo_dir):
        """Analyze code complexity using radon"""
        try:
            complexity_results = {
                'avg_complexity': 0,
                'max_complexity': 0,
                'total_functions': 0,
                'complex_functions': 0
            }
            
            # Find Python files
            python_files = []
            for root, dirs, files in os.walk(repo_dir):
                for file in files:
                    if file.endswith('.py'):
                        python_files.append(os.path.join(root, file))
                        
            if not python_files:
                return complexity_results
                
            total_complexity = 0
            total_functions = 0
            max_complexity = 0
            complex_functions = 0
            
            for py_file in python_files[:50]:  # Limit to 50 files
                try:
                    with open(py_file, 'r', encoding='utf-8') as f:
                        content = f.read()
                        
                    # Calculate complexity
                    cc_results = radon_cc.cc_visit(content)
                    
                    for result in cc_results:
                        complexity = result.complexity
                        total_complexity += complexity
                        total_functions += 1
                        
                        if complexity > max_complexity:
                            max_complexity = complexity
                            
                        if complexity > 10:  # High complexity threshold
                            complex_functions += 1
                            
                except Exception as e:
                    continue
                    
            if total_functions > 0:
                complexity_results.update({
                    'avg_complexity': total_complexity / total_functions,
                    'max_complexity': max_complexity,
                    'total_functions': total_functions,
                    'complex_functions': complex_functions,
                    'complexity_ratio': complex_functions / total_functions
                })
                
            return complexity_results
            
        except Exception as e:
            return {'error': str(e)}
            
    def analyze_test_coverage(self, repo_dir):
        """Analyze test coverage and test quality"""
        try:
            test_results = {
                'has_tests': False,
                'test_files_count': 0,
                'test_framework': 'unknown',
                'coverage_estimate': 0
            }
            
            # Find test files
            test_files = []
            for root, dirs, files in os.walk(repo_dir):
                for file in files:
                    if ('test' in file.lower() and file.endswith('.py') or
                        file.startswith('test_') or
                        'spec' in file.lower()):
                        test_files.append(os.path.join(root, file))
                        
            test_results['test_files_count'] = len(test_files)
            test_results['has_tests'] = len(test_files) > 0
            
            # Detect test framework
            if test_files:
                for test_file in test_files[:5]:
                    try:
                        with open(test_file, 'r', encoding='utf-8') as f:
                            content = f.read()
                            
                        if 'pytest' in content or 'import pytest' in content:
                            test_results['test_framework'] = 'pytest'
                        elif 'unittest' in content or 'import unittest' in content:
                            test_results['test_framework'] = 'unittest'
                        elif 'nose' in content:
                            test_results['test_framework'] = 'nose'
                            
                    except Exception as e:
                        continue
                        
            # Estimate coverage (rough heuristic)
            if test_files:
                source_files = []
                for root, dirs, files in os.walk(repo_dir):
                    for file in files:
                        if file.endswith('.py') and 'test' not in file.lower():
                            source_files.append(file)
                            
                if source_files:
                    test_results['coverage_estimate'] = min(len(test_files) / len(source_files) * 100, 100)
                    
            return test_results
            
        except Exception as e:
            return {'error': str(e)}
            
    def find_benchmark_results(self, repo_dir):
        """Find benchmark results in the repository"""
        try:
            benchmark_data = {
                'has_benchmarks': False,
                'benchmark_files': [],
                'performance_claims': [],
                'comparison_data': []
            }
            
            # Look for benchmark files and results
            benchmark_patterns = [
                'benchmark', 'performance', 'results', 'evaluation',
                'metrics', 'comparison', 'leaderboard'
            ]
            
            for root, dirs, files in os.walk(repo_dir):
                for file in files:
                    file_lower = file.lower()
                    
                    if any(pattern in file_lower for pattern in benchmark_patterns):
                        file_path = os.path.join(root, file)
                        benchmark_data['benchmark_files'].append(file)
                        
                        # Extract performance data
                        if file.endswith(('.md', '.txt', '.json', '.csv')):
                            try:
                                with open(file_path, 'r', encoding='utf-8') as f:
                                    content = f.read()
                                    
                                # Look for performance numbers
                                perf_numbers = re.findall(r'(\d+(?:\.\d+)?)\s*(fps|ms|seconds?|accuracy|f1|bleu|rouge)', content.lower())
                                if perf_numbers:
                                    benchmark_data['performance_claims'].extend(perf_numbers)
                                    
                                # Look for comparison terms
                                comparison_terms = re.findall(r'(better|faster|higher|lower|outperforms?|beats?|surpasses?)', content.lower())
                                if comparison_terms:
                                    benchmark_data['comparison_data'].extend(comparison_terms)
                                    
                            except Exception as e:
                                continue
                                
            benchmark_data['has_benchmarks'] = len(benchmark_data['benchmark_files']) > 0
            
            return benchmark_data
            
        except Exception as e:
            return {'error': str(e)}
            
    def analyze_documentation_quality(self, repo_dir):
        """Analyze documentation quality"""
        try:
            doc_quality = {
                'readme_score': 0,
                'has_docs_dir': False,
                'has_examples': False,
                'api_docs': False,
                'tutorial_count': 0
            }
            
            # Check README quality
            readme_files = ['README.md', 'README.rst', 'README.txt']
            for readme_file in readme_files:
                readme_path = os.path.join(repo_dir, readme_file)
                if os.path.exists(readme_path):
                    with open(readme_path, 'r', encoding='utf-8') as f:
                        readme_content = f.read()
                        
                    # Score README quality
                    score = 0
                    if len(readme_content) > 500: score += 2
                    if '```' in readme_content: score += 2  # Code examples
                    if re.search(r'!\[.*\]\(.*\)', readme_content): score += 2  # Images
                    if 'installation' in readme_content.lower(): score += 1
                    if 'usage' in readme_content.lower(): score += 1
                    if 'example' in readme_content.lower(): score += 1
                    if 'api' in readme_content.lower(): score += 1
                    
                    doc_quality['readme_score'] = score
                    break
                    
            # Check for docs directory
            docs_dirs = ['docs', 'documentation', 'doc']
            for docs_dir in docs_dirs:
                if os.path.exists(os.path.join(repo_dir, docs_dir)):
                    doc_quality['has_docs_dir'] = True
                    break
                    
            # Check for examples
            example_dirs = ['examples', 'example', 'demos', 'tutorials']
            for example_dir in example_dirs:
                if os.path.exists(os.path.join(repo_dir, example_dir)):
                    doc_quality['has_examples'] = True
                    
                    # Count tutorials/examples
                    example_path = os.path.join(repo_dir, example_dir)
                    for root, dirs, files in os.walk(example_path):
                        doc_quality['tutorial_count'] += len([f for f in files if f.endswith(('.py', '.ipynb', '.md'))])
                    break
                    
            return doc_quality
            
        except Exception as e:
            return {'error': str(e)}
            
    def analyze_dependencies(self, repo_dir):
        """Analyze dependencies and their quality"""
        try:
            dep_analysis = {
                'dependency_count': 0,
                'has_requirements': False,
                'has_setup_py': False,
                'dependency_quality': 0,
                'popular_deps': []
            }
            
            # Check requirements files
            req_files = ['requirements.txt', 'requirements-dev.txt', 'environment.yml', 'Pipfile']
            for req_file in req_files:
                req_path = os.path.join(repo_dir, req_file)
                if os.path.exists(req_path):
                    dep_analysis['has_requirements'] = True
                    
                    with open(req_path, 'r', encoding='utf-8') as f:
                        content = f.read()
                        
                    # Count dependencies
                    deps = [line.strip() for line in content.split('\n') if line.strip() and not line.startswith('#')]
                    dep_analysis['dependency_count'] += len(deps)
                    
                    # Check for popular/quality dependencies
                    popular_deps = ['numpy', 'pandas', 'scikit-learn', 'tensorflow', 'pytorch', 'torch']
                    for dep in deps:
                        dep_name = dep.split('==')[0].split('>=')[0].split('<=')[0].strip()
                        if dep_name.lower() in popular_deps:
                            dep_analysis['popular_deps'].append(dep_name)
                            
            # Check setup.py
            if os.path.exists(os.path.join(repo_dir, 'setup.py')):
                dep_analysis['has_setup_py'] = True
                
            # Calculate dependency quality score
            score = 0
            if dep_analysis['has_requirements']: score += 2
            if dep_analysis['has_setup_py']: score += 2
            if len(dep_analysis['popular_deps']) > 0: score += 2
            if dep_analysis['dependency_count'] < 20: score += 1  # Not too many deps
            
            dep_analysis['dependency_quality'] = score
            
            return dep_analysis
            
        except Exception as e:
            return {'error': str(e)}
            
    def analyze_architecture(self, repo_dir):
        """Analyze code architecture and structure"""
        try:
            arch_analysis = {
                'directory_structure_score': 0,
                'module_organization': 0,
                'has_main_entry': False,
                'separation_of_concerns': 0
            }
            
            # Analyze directory structure
            dirs = []
            files = []
            for item in os.listdir(repo_dir):
                if os.path.isdir(os.path.join(repo_dir, item)) and not item.startswith('.'):
                    dirs.append(item)
                elif os.path.isfile(os.path.join(repo_dir, item)):
                    files.append(item)
                    
            # Good structure indicators
            good_dirs = ['src', 'lib', 'tests', 'docs', 'examples', 'scripts', 'utils']
            structure_score = sum(1 for d in dirs if d.lower() in good_dirs)
            arch_analysis['directory_structure_score'] = min(structure_score, 5)
            
            # Check for main entry point
            main_files = ['main.py', 'app.py', '__main__.py', 'run.py']
            arch_analysis['has_main_entry'] = any(f in files for f in main_files)
            
            return arch_analysis
            
        except Exception as e:
            return {'error': str(e)}
            
    def find_performance_indicators(self, repo_dir):
        """Find performance indicators and optimizations"""
        try:
            perf_indicators = {
                'has_profiling': False,
                'optimization_keywords': 0,
                'performance_tests': False,
                'memory_optimizations': False
            }
            
            # Search for performance-related files and content
            perf_keywords = ['optimization', 'performance', 'speed', 'efficient', 'fast', 'cache', 'memory']
            opt_keywords = ['vectorize', 'parallel', 'multithread', 'async', 'batch', 'gpu', 'cuda']
            
            for root, dirs, files in os.walk(repo_dir):
                for file in files:
                    if file.endswith('.py'):
                        try:
                            with open(os.path.join(root, file), 'r', encoding='utf-8') as f:
                                content = f.read().lower()
                                
                            # Count performance keywords
                            perf_indicators['optimization_keywords'] += sum(content.count(kw) for kw in perf_keywords)
                            
                            # Check for optimization techniques
                            if any(kw in content for kw in opt_keywords):
                                perf_indicators['memory_optimizations'] = True
                                
                            # Check for profiling
                            if 'profile' in content or 'cprofile' in content or 'timeit' in content:
                                perf_indicators['has_profiling'] = True
                                
                        except Exception as e:
                            continue
                            
            return perf_indicators
            
        except Exception as e:
            return {'error': str(e)}
            
    def deep_code_analysis(self):
        """Perform ongoing deep code analysis"""
        while self.running:
            try:
                # Generate reports on analyzed repositories
                self.generate_quality_report()
                
                # Compare repositories
                self.compare_implementations()
                
                # Update benchmarks
                self.update_benchmark_data()
                
                time.sleep(3600)  # Generate reports every hour
                
            except Exception as e:
                print(f"Deep analysis error: {e}")
                time.sleep(600)
                
    def generate_quality_report(self):
        """Generate quality report for analyzed repositories"""
        try:
            completed_analyses = {repo: data for repo, data in self.code_quality_metrics.items() 
                                if data.get('status') == 'completed'}
            
            if not completed_analyses:
                return
                
            # Calculate scores for each repository
            repo_scores = {}
            
            for repo_path, analysis in completed_analyses.items():
                results = analysis.get('analysis_results', {})
                
                # Calculate overall quality score
                score = 0
                
                # Complexity score (lower is better)
                complexity = results.get('complexity', {})
                if complexity.get('avg_complexity', 0) < 5:
                    score += 20
                elif complexity.get('avg_complexity', 0) < 10:
                    score += 10
                    
                # Test coverage score
                test_cov = results.get('test_coverage', {})
                if test_cov.get('has_tests', False):
                    score += 15
                if test_cov.get('coverage_estimate', 0) > 70:
                    score += 15
                    
                # Documentation score
                docs = results.get('documentation', {})
                score += docs.get('readme_score', 0)
                if docs.get('has_examples', False):
                    score += 10
                    
                # Architecture score
                arch = results.get('architecture', {})
                score += arch.get('directory_structure_score', 0) * 2
                if arch.get('has_main_entry', False):
                    score += 5
                    
                # Performance indicators
                perf = results.get('performance_indicators', {})
                if perf.get('has_profiling', False):
                    score += 10
                if perf.get('memory_optimizations', False):
                    score += 10
                    
                # Benchmark bonus
                bench = results.get('benchmarks', {})
                if bench.get('has_benchmarks', False):
                    score += 15
                    
                repo_scores[repo_path] = score
                
            # Save quality report
            quality_report = {
                'top_repositories': sorted(repo_scores.items(), key=lambda x: x[1], reverse=True)[:10],
                'analysis_summary': {
                    'total_analyzed': len(completed_analyses),
                    'avg_score': sum(repo_scores.values()) / len(repo_scores) if repo_scores else 0,
                    'generated_at': datetime.now().isoformat()
                }
            }
            
            with open('/Users/jamestunick/xrai/quality_report.json', 'w') as f:
                json.dump(quality_report, f, indent=2)
                
        except Exception as e:
            print(f"Quality report error: {e}")
            
    def compare_implementations(self):
        """Compare different implementations of similar functionality"""
        # This would compare repositories that solve similar problems
        # and identify which implementations are superior
        pass
        
    def update_benchmark_data(self):
        """Update benchmark data from various sources"""
        # This would fetch the latest benchmark results from leaderboards
        # and update our comparisons
        pass
        
    def get_quality_recommendations(self, repo_type="general"):
        """Get quality recommendations based on deep analysis"""
        recommendations = []
        
        if os.path.exists('/Users/jamestunick/xrai/quality_report.json'):
            with open('/Users/jamestunick/xrai/quality_report.json', 'r') as f:
                report = json.load(f)
                
            top_repos = report.get('top_repositories', [])
            
            if top_repos:
                recommendations.append("🏆 Top Quality Repositories (Deep Analysis):")
                for repo_path, score in top_repos[:5]:
                    recommendations.append(f"  {repo_path} (Score: {score}/100)")
                    
        recommendations.extend([
            "\n🔬 Quality Factors Found in Top Repositories:",
            "  • Comprehensive test suites (>70% coverage)",
            "  • Low cyclomatic complexity (<5 avg)",
            "  • Clear documentation with examples",
            "  • Performance benchmarks included",
            "  • Clean architecture with separation of concerns",
            "  • Profiling and optimization evidence",
            "  • Popular, well-maintained dependencies"
        ])
        
        return recommendations

if __name__ == "__main__":
    print("🔬 Starting Deep Code Analyzer...")
    print("📊 Analyzing repositories beyond stars - focusing on code quality and benchmarks...")
    
    analyzer = DeepCodeAnalyzer()
    
    try:
        while True:
            print(f"\n📈 Analysis Status:")
            print(f"  Leaderboard repos found: {len(analyzer.leaderboard_repos)}")
            print(f"  Repositories analyzed: {len([r for r in analyzer.code_quality_metrics.values() if r.get('status') == 'completed'])}")
            print(f"  Currently analyzing: {len([r for r in analyzer.code_quality_metrics.values() if r.get('status') == 'analyzing'])}")
            
            time.sleep(300)  # Status update every 5 minutes
            
    except KeyboardInterrupt:
        analyzer.running = False
        print("\n🛑 Deep Code Analyzer stopped")