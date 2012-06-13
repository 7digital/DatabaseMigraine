require 'albacore'
require 'FileUtils'

BUILDTOOLS		= "C:/TeamCityBuildTools"

require File.expand_path("#{File.dirname(__FILE__)}/build/build.rb")
require File.expand_path("#{File.dirname(__FILE__)}/build/nunit_test.rb")
require File.expand_path("#{File.dirname(__FILE__)}/build/metrics.rb")
require File.expand_path("#{File.dirname(__FILE__)}/build/lib/source_index.rb")
begin
	require File.expand_path("#{File.dirname(__FILE__)}/build/lib/update_dependencies.rb")
	require File.expand_path("#{File.dirname(__FILE__)}/build/lib/update_shared_dbs.rb")
	require File.expand_path("#{File.dirname(__FILE__)}/build/rake/migration_tasks.rb")
rescue LoadError
	#no problem, we simply are not in the dependencyChain or generateMigrations build (TeamCity)
end

SOLUTION			= "DatabaseMigraine"

task :build_and_test, :test_type, :test_env do |t, args|
	Rake::Task["build_solution"].invoke
	Rake::Task["run_unit_tests"].invoke(args.test_type, args.test_env)
end

