using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UniFlowGW
{
	public class Template
	{

		public static string tickettempPdf = @"
			<MOMJOB>
				<Identity type=""LDAPLogin"">$USERID$</Identity>
					<IfUnknown>Create</IfUnknown>          <!-- empty or absent: Reject -->
					<JobFile type='PDF'>$PATH$</JobFile>
					<JobName>$FILENAME$</JobName>  
					<Ticket>                            <!-- optional -->
					<Copies>$COPIES$</Copies>                      <!-- empty or absent: 1 -->
					<ColorMode>$COLORMODE$</ColorMode>        <!-- empty or absent: Auto -->
					<PageSize>$PAPERSIZE$</PageSize>
					<Duplex>$DUPLEX$</Duplex> <!-- empty or absent: Simplex -->
					</Ticket>
			</MOMJOB>";

		public static string ticketPdf = @"
			<MOMJOB>
				<Identity type=""LDAPLogin"">$USERID$</Identity>
					<IfUnknown>Create</IfUnknown>          <!-- empty or absent: Reject -->
					<JobFile type='PDF'>$PATH$</JobFile>
					<JobName>$FILENAME$</JobName>  
					<Ticket>                            <!-- optional -->
					<Copies>$COPIES$</Copies>                      <!-- empty or absent: 1 -->
					<ColorMode>$COLORMODE$</ColorMode>        <!-- empty or absent: Auto -->
					<PageSize>$PAPERSIZE$</PageSize>
					</Ticket>
			</MOMJOB>";
		public static string tickettempImage = @"
			<MOMJOB>
				<Identity type=""LDAPLogin"">$USERID$</Identity>
				<IfUnknown>Create</IfUnknown>          <!-- empty or absent: Reject -->
				<JobFile type=""Native"">$PATH$</JobFile> 
				<JobName>$FILENAME$</JobName>  
				<Ticket>                            <!-- optional -->
				<Copies>$COPIES$</Copies>                      <!-- empty or absent: 1 -->
				</Ticket>
			</MOMJOB>";
	}
}
