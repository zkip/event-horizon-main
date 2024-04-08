# 删除它自带的私有仓库，其他人访问不了，不要也可以正常运行，见 https://github.com/PavelZinchenko/event-horizon-main/issues/325#issuecomment-2043104776
# Delete its own private warehouse, others can not access, without this also can work.
git rm Assets/ModulesPrivate

# 同步其它的子模块
# Synchronize other submodules
git submodule update --init --recursive