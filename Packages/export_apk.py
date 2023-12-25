#!/bin/python
#-*-coding:utf-8-*-

import os
import os.path
import subprocess

import sys

import time

import subprocess

def buildApk(tag):

	current_directory = os.path.abspath(os.path.join(os.path.dirname(__file__),'..','..', 'build', 'crushBlock'))
	os.chdir(current_directory)
	print("current_directory " + current_directory)

	print(os.environ.get('PATH'))

	# os.system('chmod 777 gradlew')

	gradle_dir = '/usr/local/bin/gradle-7.2/bin/gradle'

	print('gradle clean')
	os.system(gradle_dir + ' clean')
	print('gradle assemble, generate apk')

	if not os.path.exists('output.txt'):
	    open('output.txt', 'w').close()

	os.system('tail -f output.txt &')
	os.system(gradle_dir + ' assembleJewelslidingC_KDebug --stacktrace > output.txt')

	print('Generate apk finish!')


def build_project(gradle_path, project_path):
    current_directory = os.path.abspath(project_path)
    os.chdir(current_directory)
    print("current_directory " + current_directory)
    
    print(os.environ.get('PATH'))

    # 组装 gradle 命令
    command = f"{gradle_path} -p {project_path} clean assembleJewelslidingC_KDebug"
    print('gradle zzz' + command)
    # 执行 gradle 命令
    result = subprocess.run(command, shell=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    
    # 将 stdout 和 stderr 输出到控制台
    print(result.stdout.decode())
    print(result.stderr.decode())

build_project("/usr/local/bin/gradle-7.2/bin/gradle", "/Users/blowfire/app_jewel_unity/App_JewelBlast/jb_unity/build/Jewel_2021_cn_new")

# if __name__ == '__main__':
#     buildApk(sys.argv[1])
    
