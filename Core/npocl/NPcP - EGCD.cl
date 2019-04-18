typedef struct _Parameter {
	int a;
	int b;
} Parameter;

typedef struct _Solution {
	int gcd;
	int x;
	int y;
} Solution;

__kernel void solve(__global Parameter * parameters, __global Solution * solutions){
	int index = get_global_id(0);
	Parameter param = parameters[index];
	int s = 0;
	int r = param.b;
	int prevS = 1;
	int prevR = param.a;
	while(r != 0){
		int q = prevR/r;
		int tmp = r;
		r = prevR - q*r;
		prevR = tmp;
		tmp = s;
		s = prevS - q*s;
		prevS = tmp;
	}
	int bezT = 0;
	if(param.b != 0) bezT = (prevR-prevS*param.a)/param.b;
	solutions[index].gcd = prevR;
	solutions[index].x = prevS;
	solutions[index].y = bezT;
}

__kernel void check(__global Parameter * parameters, __global Solution * solutions, __global short * output){
	int index = get_global_id(0);
	Parameter param = parameters[index];
	Solution sol = solutions[index];
	output[index] = param.a*sol.x + param.b*sol.y == sol.gcd ? 1 : 0;
}